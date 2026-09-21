using FCQI.Application.Common;
using FCQI.Application.Sessions.Commands;
using FCQI.Application.Sessions.Queries;
using FCQI.Domain;
using FCQI.Domain.Entities;
using FCQI.Infrastructure.Persistence;
using FCQI.Infrastructure.Time;
using FCQI.UnitTests.Persistence;

namespace FCQI.UnitTests.Auth;

/// <summary>
/// Reglas de quién puede tocar qué asesoría. Antes no había ninguna: cualquier
/// petición sin token podía cancelar la cita de otro alumno.
/// </summary>
public class SessionAuthorizationTests
{
    private const int Advisor = 1;
    private const int Student = 2;
    private const int Intruder = 3;
    private const int AdminId = 4;

    private static Caller AsAdvisor(int id = Advisor) => new(id, false, true, false);
    private static Caller AsStudent(int id = Student) => new(id, false, false, true);
    private static Caller AsAdmin() => new(AdminId, true, false, false);

    [Fact]
    public async Task OwningAdvisor_CanConfirm()
    {
        await using var db = await SeedAsync();
        await Handler(db).HandleAsync(1, SessionStatuses.Confirmed, AsAdvisor());

        Assert.Equal(SessionStatusIds.Confirmed, db.AdvisorySessions.Single().StatusId);
    }

    [Fact]
    public async Task OwningStudent_CanCancelTheirOwn()
    {
        await using var db = await SeedAsync();
        await Handler(db).HandleAsync(1, SessionStatuses.Cancelled, AsStudent());

        Assert.Equal(SessionStatusIds.Cancelled, db.AdvisorySessions.Single().StatusId);
    }

    [Fact]
    public async Task OwningStudent_CannotConfirmTheirOwn()
    {
        await using var db = await SeedAsync();

        await Assert.ThrowsAsync<ForbiddenOperationException>(
            () => Handler(db).HandleAsync(1, SessionStatuses.Confirmed, AsStudent()));
    }

    [Fact]
    public async Task Stranger_CannotTouchSomeoneElsesSession()
    {
        await using var db = await SeedAsync();

        await Assert.ThrowsAsync<ForbiddenOperationException>(
            () => Handler(db).HandleAsync(1, SessionStatuses.Cancelled, AsStudent(Intruder)));
    }

    [Fact]
    public async Task OtherAdvisor_CannotTouchAnotherAdvisorsSession()
    {
        await using var db = await SeedAsync();

        await Assert.ThrowsAsync<ForbiddenOperationException>(
            () => Handler(db).HandleAsync(1, SessionStatuses.Rejected, AsAdvisor(Intruder)));
    }

    [Fact]
    public async Task Admin_CanDoAnything()
    {
        await using var db = await SeedAsync();
        await Handler(db).HandleAsync(1, SessionStatuses.Rejected, AsAdmin());

        Assert.Equal(SessionStatusIds.Rejected, db.AdvisorySessions.Single().StatusId);
    }

    [Fact]
    public async Task Query_ScopesAStudentToTheirOwnSessions()
    {
        await using var db = await SeedAsync();
        var result = await Query(db).HandleAsync(null, null, null, AsStudent());

        Assert.Single(result);
        Assert.All(result, s => Assert.Equal(Student, s.StudentId));
    }

    [Fact]
    public async Task Query_RejectsAskingForSomeoneElsesSessions()
    {
        await using var db = await SeedAsync();

        await Assert.ThrowsAsync<ForbiddenOperationException>(
            () => Query(db).HandleAsync(Intruder, null, null, AsStudent()));
    }

    [Fact]
    public async Task Query_LetsAdminSeeEverything()
    {
        await using var db = await SeedAsync();
        var result = await Query(db).HandleAsync(null, null, null, AsAdmin());

        Assert.Single(result);
    }

    private static UpdateSessionStatusCommandHandler Handler(AppDbContext db) => new(db);
    private static GetSessionsQueryHandler Query(AppDbContext db) => new(db, new CampusClock());

    /// <summary>
    /// Siembra el grafo completo. Las consultas proyectan sobre las
    /// navegaciones (nombre del alumno, del asesor, de la materia), así que
    /// sembrar solo la sesión devolvía una lista vacía y los asertos pasaban
    /// en falso sobre una colección sin elementos.
    /// </summary>
    private static async Task<AppDbContext> SeedAsync()
    {
        var db = TestContext.Create();

        db.SessionStatuses.AddRange(
            new SessionStatus { Id = 1, Code = "PENDING", Name = SessionStatuses.Pending, IsActive = true },
            new SessionStatus { Id = 2, Code = "CONFIRMED", Name = SessionStatuses.Confirmed, IsActive = true },
            new SessionStatus { Id = 3, Code = "CANCELLED", Name = SessionStatuses.Cancelled, IsActive = false },
            new SessionStatus { Id = 4, Code = "REJECTED", Name = SessionStatuses.Rejected, IsActive = false });

        db.Terms.Add(new AcademicTerm
        {
            Id = 1, Code = "2026-2", Name = "Otoño 2026",
            StartsOn = new DateOnly(2026, 8, 10), EndsOn = new DateOnly(2026, 12, 11), IsCurrent = true
        });
        db.Programs.Add(new AcademicProgram { Id = 1, Code = "IQ", Name = "Ingeniería Química" });
        db.Modalities.Add(new Modality { Id = 1, Code = "IN_PERSON", Name = "Presencial" });
        db.Locations.Add(new Location { Id = 1, Name = "Cubículo FCQI", ModalityId = 1 });
        db.Subjects.Add(new Subject { Id = 1, Code = "CALCULO-DIFERENCIAL", Name = "Cálculo Diferencial" });

        db.People.AddRange(
            Person(Advisor, "Felipe", "Márquez", "Vizcarra", "felipe.marquez63@uabc.edu.mx"),
            Person(Student, "Yesua", "Díaz", "Hernández", "yesua.diaz@uabc.edu.mx"),
            Person(Intruder, "Ajeno", "Pérez", "López", "ajeno.perez@uabc.edu.mx"));

        db.AdvisorProfiles.Add(new AdvisorProfile { PersonId = Advisor, ProgramId = 1, DefaultModalityId = 1 });
        db.StudentProfiles.AddRange(
            new StudentProfile { PersonId = Student, StudentNumber = "2208134" },
            new StudentProfile { PersonId = Intruder, StudentNumber = "2299999" });

        db.Availabilities.Add(new Availability
        {
            Id = 1, TermId = 1, AdvisorId = Advisor, DayOfWeek = DayOfWeek.Wednesday,
            StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(16, 0),
            MaxCapacity = 1, LocationId = 1
        });

        db.AdvisorySessions.Add(new AdvisorySession
        {
            Id = 1,
            TermId = 1,
            AvailabilityId = 1,
            AdvisorId = Advisor,
            StudentId = Student,
            SubjectId = 1,
            ScheduledAt = new DateTime(2026, 10, 7, 19, 0, 0, DateTimeKind.Utc),
            SeatNumber = 1,
            StatusId = SessionStatusIds.Pending,
            Topic = "Prueba"
        });

        await db.SaveChangesAsync();
        return db;
    }

    private static Person Person(int id, string first, string paternal, string maternal, string email)
    {
        var person = new Person
        {
            Id = id, FirstName = first, LastNamePaternal = paternal,
            LastNameMaternal = maternal, Email = email, IsActive = true
        };

        // DisplayName lo genera MySQL; el proveedor en memoria no lo calcula.
        typeof(Person).GetProperty(nameof(Domain.Entities.Person.DisplayName))!
            .SetValue(person, $"{first} {paternal} {maternal}");

        return person;
    }
}
