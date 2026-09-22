using FCQI.Application.Common;
using FCQI.Application.Interfaces;
using FCQI.Application.Sessions.Dtos;
using FCQI.Domain;
using FCQI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Sessions.Commands;

public class CreateSessionCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly CurrentTerm _term;
    private readonly IClock _clock;

    public CreateSessionCommandHandler(IApplicationDbContext context, CurrentTerm term, IClock clock)
    {
        _context = context;
        _term = term;
        _clock = clock;
    }

    public async Task<SessionDto> HandleAsync(
        CreateSessionRequest request, Caller caller, CancellationToken cancellationToken = default)
    {
        var term = await _term.GetAsync(cancellationToken);
        var termId = term.Id;

        // A nombre de quién se agenda. El alumno solo puede agendar para sí:
        // el id sale del token, no del cuerpo de la petición, que es lo que
        // antes permitía suplantar a otro. Dirección sí puede agendar por un
        // alumno, que es parte de su trabajo en ventanilla.
        int studentId;
        if (caller.IsAdmin)
        {
            studentId = request.StudentId;
        }
        else if (caller.IsStudent)
        {
            studentId = caller.PersonId;
        }
        else
        {
            throw new ForbiddenOperationException(
                "Solo un alumno puede solicitar una asesoría.");
        }

        var studentExists = await _context.StudentProfiles
            .AnyAsync(s => s.PersonId == studentId && s.IsActive, cancellationToken);
        if (!studentExists)
        {
            throw new InvalidOperationException("El alumno no está dado de alta en el programa.");
        }

        // Los asesores pares son alumnos que asesoran, así que una misma
        // persona puede aparecer en los dos extremos de la petición. Nada en
        // la base lo impide —son dos columnas distintas— y sin esta regla un
        // asesor par podía ocupar su propio bloque de horario como alumno.
        if (studentId == request.AdvisorId)
        {
            throw new InvalidOperationException("No puedes agendar una asesoría contigo mismo.");
        }

        var slot = await _context.Availabilities
                       .AsNoTracking()
                       .SingleOrDefaultAsync(a => a.Id == request.AvailabilityId, cancellationToken)
                   ?? throw new InvalidOperationException("El horario no existe.");

        if (!slot.IsActive)
        {
            throw new InvalidOperationException("Ese horario ya no está disponible.");
        }

        if (slot.TermId != termId)
        {
            throw new InvalidOperationException("Ese horario pertenece a un ciclo escolar anterior.");
        }

        // La base ya impide que el asesor no sea el dueño del bloque, mediante
        // una llave foránea compuesta. Se comprueba aquí también para devolver
        // un mensaje claro en vez de un error de restricción.
        if (slot.AdvisorId != request.AdvisorId)
        {
            throw new InvalidOperationException("El horario no pertenece a ese tutor.");
        }

        var teaches = await _context.AdvisorSubjects.AnyAsync(
            x => x.TermId == termId
                 && x.AdvisorId == request.AdvisorId
                 && x.SubjectId == request.SubjectId, cancellationToken);
        if (!teaches)
        {
            throw new InvalidOperationException("El directivo no ha asignado esta materia a ese tutor.");
        }

        // La fecha llega en hora local del campus. Debe caer en el día y la
        // hora de inicio del bloque: esta validación vive aquí y no en la base
        // porque comprobarla en SQL exige CONVERT_TZ, que devuelve NULL si las
        // tablas de zona horaria de MySQL no están cargadas.
        var local = DateTime.SpecifyKind(request.ScheduledAt, DateTimeKind.Unspecified);

        if (local.DayOfWeek != slot.DayOfWeek)
        {
            throw new InvalidOperationException(
                $"La fecha elegida es {DayName(local.DayOfWeek)} y ese horario es de {DayName(slot.DayOfWeek)}.");
        }

        if (TimeOnly.FromDateTime(local) != slot.StartTime)
        {
            throw new InvalidOperationException(
                $"Ese horario empieza a las {slot.StartTime:HH\\:mm}.");
        }

        var scheduledUtc = _clock.ToUtc(local);

        if (scheduledUtc <= _clock.UtcNow)
        {
            throw new InvalidOperationException("No se puede agendar una asesoría en el pasado.");
        }

        // ciclos_escolares define la ventana del ciclo. Sin esta comprobación se
        // podían agendar asesorías para un semestre que ni siquiera existe.
        var day = DateOnly.FromDateTime(local);
        if (day < term.StartsOn || day > term.EndsOn)
        {
            throw new InvalidOperationException(
                $"La fecha está fuera del ciclo {term.Code}, que va del " +
                $"{term.StartsOn:dd/MM/yyyy} al {term.EndsOn:dd/MM/yyyy}.");
        }

        // Un lugar libre dentro del cupo. ActiveAt vale NULL en las sesiones
        // canceladas o rechazadas, así que esas no ocupan asiento.
        var taken = await _context.AdvisorySessions
            .Where(s => s.AvailabilityId == slot.Id && s.ActiveAt == scheduledUtc)
            .Select(s => s.SeatNumber)
            .ToListAsync(cancellationToken);

        // El duplicado se comprueba antes que el cupo: si el lugar lo ocupa el
        // propio alumno, "ya tienes una solicitud" explica mejor que "lleno".
        var mine = await _context.AdvisorySessions.AnyAsync(
            s => s.StudentId == studentId
                 && s.AvailabilityId == slot.Id
                 && s.ActiveAt == scheduledUtc, cancellationToken);
        if (mine)
        {
            throw new InvalidOperationException("Ya tienes una solicitud activa en ese horario.");
        }

        if (taken.Count >= slot.MaxCapacity)
        {
            throw new InvalidOperationException("Ese horario ya está lleno.");
        }

        var seat = Enumerable.Range(1, slot.MaxCapacity).First(n => !taken.Contains(n));

        var session = new AdvisorySession
        {
            TermId = termId,
            AvailabilityId = slot.Id,
            AdvisorId = request.AdvisorId,
            StudentId = studentId,
            SubjectId = request.SubjectId,
            ScheduledAt = scheduledUtc,
            SeatNumber = seat,
            StatusId = SessionStatusIds.Pending,
            Topic = string.IsNullOrWhiteSpace(request.Topic) ? "Asesoría" : request.Topic.Trim()
        };

        _context.AdvisorySessions.Add(session);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // La causa más probable es una carrera: entre la comprobación de
            // cupo y el INSERT se coló otra solicitud, y los índices únicos de
            // la base tuvieron la última palabra. Pero no es la única causa
            // posible, así que el mensaje no la afirma como si lo fuera.
            throw new InvalidOperationException(
                "No se pudo registrar la asesoría; es posible que ese lugar " +
                "acabe de ocuparse. Vuelve a intentarlo.", ex);
        }

        return await MapAsync(session.Id, cancellationToken);
    }

    private static string DayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Sunday => "domingo",
        DayOfWeek.Monday => "lunes",
        DayOfWeek.Tuesday => "martes",
        DayOfWeek.Wednesday => "miércoles",
        DayOfWeek.Thursday => "jueves",
        DayOfWeek.Friday => "viernes",
        _ => "sábado"
    };

    private async Task<SessionDto> MapAsync(long id, CancellationToken cancellationToken)
    {
        var s = await _context.AdvisorySessions
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.StudentId,
                StudentName = x.Student.Person.DisplayName,
                x.AdvisorId,
                AdvisorName = x.Advisor.Person.DisplayName,
                x.SubjectId,
                SubjectName = x.Subject.Name,
                x.AvailabilityId,
                x.ScheduledAt,
                x.Topic,
                Status = x.Status.Name
            })
            .SingleAsync(cancellationToken);

        return new SessionDto(
            (int)s.Id, s.StudentId, s.StudentName,
            s.AdvisorId, s.AdvisorName,
            s.SubjectId, s.SubjectName,
            s.AvailabilityId,
            _clock.ToLocal(s.ScheduledAt),
            DateTime.SpecifyKind(s.ScheduledAt, DateTimeKind.Utc),
            s.Topic, s.Status);
    }
}
