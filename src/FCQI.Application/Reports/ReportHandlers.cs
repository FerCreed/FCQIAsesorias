using FCQI.Application.Common;
using FCQI.Application.Interfaces;
using FCQI.Application.Sessions;
using FCQI.Domain;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Reports;

// Reporte individual de una asesoría: lo descargan sus participantes y dirección.
public class SessionReportQueryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IClock _clock;

    // Recibe el contexto de datos y el reloj (para convertir a hora local) por inyección.
    public SessionReportQueryHandler(IApplicationDbContext context, IClock clock)
    {
        _context = context;
        _clock = clock;
    }

    // Arma el comprobante de la asesoría con su historial de estados. Primero
    // verifica que quien llama participe en ella, para no exponer datos de
    // terceros por identificador.
    public async Task<SessionReport> HandleAsync(long sessionId, Caller caller, CancellationToken cancellationToken = default)
    {
        // Carga mínima solo para aplicar la política de acceso antes de proyectar el reporte.
        var session = await _context.AdvisorySessions.AsNoTracking()
                          .SingleOrDefaultAsync(s => s.Id == sessionId, cancellationToken)
                      ?? throw new NotFoundException("La asesoría no existe.");
        SessionPolicy.RequireActor(session, caller);

        // Reutiliza la proyección común de reportes para que el comprobante coincida con las exportaciones.
        var record = (await _context.AdvisorySessions.Where(s => s.Id == sessionId).ToRecordsAsync(_clock, cancellationToken)).Single();

        // ChangedBy no tiene navegación, así que el nombre del autor se resuelve con una subconsulta.
        var history = await _context.SessionStatusChanges.AsNoTracking()
            .Where(h => h.SessionId == sessionId)
            .OrderBy(h => h.ChangedAt).ThenBy(h => h.Id)
            .Select(h => new
            {
                h.ChangedAt,
                From = h.FromStatus != null ? h.FromStatus.Name : null,
                To = h.ToStatus.Name,
                By = _context.People.Where(p => p.Id == h.ChangedBy).Select(p => p.DisplayName).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return new SessionReport(
            record,
            history.Select(h => new StatusChangeRecord(_clock.ToLocal(h.ChangedAt), h.From, h.To, h.By)).ToList(),
            _clock.LocalNow);
    }
}

// Resumen de un asesor: sus asesorías del periodo (por fecha de la cita) y sus
// horas trabajadas. El asesor solo puede pedir el suyo; dirección, el de cualquiera.
public class AdvisorSummaryQueryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IClock _clock;

    // Recibe el contexto de datos y el reloj por inyección.
    public AdvisorSummaryQueryHandler(IApplicationDbContext context, IClock clock)
    {
        _context = context;
        _clock = clock;
    }

    // Devuelve el resumen del asesor entre from y
    // to (fechas locales, ambas inclusive).
    public async Task<AdvisorSummary> HandleAsync(
        int advisorId, DateOnly from, DateOnly to, Caller caller, CancellationToken cancellationToken = default)
    {
        if (!caller.IsAdmin && !(caller.IsAdvisor && caller.PersonId == advisorId))
        {
            throw new ForbiddenOperationException("Solo puedes descargar tu propio resumen de asesorías.");
        }

        var period = ReportRules.Period(from, to);
        var advisor = await _context.AdvisorProfiles.AsNoTracking()
                          .Where(a => a.PersonId == advisorId)
                          .Select(a => new
                          {
                              a.Person.DisplayName,
                              a.Person.Email,
                              IsStudent = a.Person.StudentProfile != null && a.Person.StudentProfile.IsActive
                          })
                          .SingleOrDefaultAsync(cancellationToken)
                      ?? throw new NotFoundException("El asesor no existe.");

        // El periodo se convierte a UTC para comparar contra ScheduledAt, que se guarda en UTC.
        var (fromUtc, toUtc) = period.ToUtcRange(_clock);
        var rows = await _context.AdvisorySessions
            .Where(s => s.AdvisorId == advisorId && s.ScheduledAt >= fromUtc && s.ScheduledAt < toUtc)
            .ToRecordsAsync(_clock, cancellationToken);

        return new AdvisorSummary(
            advisor.DisplayName, advisor.Email, advisor.IsStudent ? AdvisorKinds.Student : AdvisorKinds.Professor,
            period, ReportMetrics.From(rows), rows, _clock.LocalNow);
    }
}

// Todas las asesorías creadas en el sistema desde una fecha (BPMN 6), con sus
// métricas. Solo dirección.
public class SessionsExportQueryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IClock _clock;

    // Recibe el contexto de datos y el reloj por inyección.
    public SessionsExportQueryHandler(IApplicationDbContext context, IClock clock)
    {
        _context = context;
        _clock = clock;
    }

    // Devuelve las asesorías creadas en el periodo, filtradas opcionalmente por
    // asesor, materia y estado. Sin fecha final, el periodo llega hasta hoy.
    public async Task<SessionsExport> HandleAsync(ReportFilter filter, Caller caller, CancellationToken cancellationToken = default)
    {
        if (!caller.IsAdmin)
        {
            throw new ForbiddenOperationException("El reporte de todas las asesorías es solo para dirección.");
        }

        var period = ReportRules.Period(filter.From, filter.To ?? DateOnly.FromDateTime(_clock.LocalNow));
        var (fromUtc, toUtc) = period.ToUtcRange(_clock);

        // Filtra por fecha de creación (demanda del periodo) y compone los filtros opcionales sobre la misma consulta.
        var query = _context.AdvisorySessions.Where(s => s.CreatedAt >= fromUtc && s.CreatedAt < toUtc);
        if (filter.AdvisorId is not null) query = query.Where(s => s.AdvisorId == filter.AdvisorId);
        if (filter.SubjectId is not null) query = query.Where(s => s.SubjectId == filter.SubjectId);
        if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(s => s.Status.Name == filter.Status);

        var rows = await query.ToRecordsAsync(_clock, cancellationToken);
        return new SessionsExport(period, ReportMetrics.From(rows), rows, _clock.LocalNow);
    }
}

// Validaciones compartidas por los reportes; interna porque no es parte del contrato público.
internal static class ReportRules
{
    // Construye el periodo validando que ambas fechas existan y estén en orden.
    // default indica una fecha que no llegó en la petición.
    public static ReportPeriod Period(DateOnly from, DateOnly to)
    {
        if (from == default || to == default)
        {
            throw new BusinessRuleException("Indica la fecha inicial y la final del reporte.");
        }

        if (to < from)
        {
            throw new BusinessRuleException("La fecha final debe ser igual o posterior a la inicial.");
        }

        return new ReportPeriod(from, to);
    }
}
