using FCQI.Application.Common;
using FCQI.Domain;
using FCQI.Domain.Entities;
using FCQI.Infrastructure.Persistence;
using FCQI.UnitTests.Persistence;

namespace FCQI.UnitTests.Processes;

/// <summary>
/// Escenario común de los procesos BPMN nuevos. "Ahora" es el lunes
/// 5/oct/2026 12:00 en Ensenada (19:00 UTC); la cita sembrada es el miércoles
/// 7/oct a las 12:00 locales, 48 horas después.
/// </summary>
internal static class ProcessWorld
{
    public const int Advisor = 1;
    public const int Student = 2;
    public const int Admin = 3;
    public const int InPersonBlock = 1;   // miércoles 12:00–14:00, presencial
    public const int VirtualBlock = 2;    // viernes 10:00–12:00, virtual
    public const string RoomLink = "https://meet.google.com/fcq-sala-virtual";

    public static readonly DateTime Now = new(2026, 10, 5, 19, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime WednesdaySession = new(2026, 10, 7, 19, 0, 0, DateTimeKind.Utc);

    public static Caller AsAdvisor() => new(Advisor, false, true, false);
    public static Caller AsStudent() => new(Student, false, false, true);
    public static Caller AsAdmin() => new(Admin, true, false, false);

    public static TestClock Clock(DateTime? utcNow = null) => new(utcNow ?? Now);

    public static async Task<AppDbContext> SeedAsync(byte status = SessionStatusIds.Pending, int block = InPersonBlock)
    {
        var db = TestContext.Create();

        db.SessionStatuses.AddRange(
            new SessionStatus { Id = 1, Code = "PENDING", Name = SessionStatuses.Pending, IsActive = true },
            new SessionStatus { Id = 2, Code = "CONFIRMED", Name = SessionStatuses.Confirmed, IsActive = true },
            new SessionStatus { Id = 3, Code = "CANCELLED", Name = SessionStatuses.Cancelled, IsActive = false },
            new SessionStatus { Id = 4, Code = "REJECTED", Name = SessionStatuses.Rejected, IsActive = false },
            new SessionStatus { Id = 5, Code = "COMPLETED", Name = SessionStatuses.Completed, IsActive = false });

        db.Terms.Add(new AcademicTerm
        {
            Id = 1, Code = "2026-2", Name = "Otoño 2026",
            StartsOn = new DateOnly(2026, 8, 10), EndsOn = new DateOnly(2026, 12, 11), IsCurrent = true
        });
        db.Programs.Add(new AcademicProgram { Id = 1, Code = "IQ", Name = "Ingeniería Química" });
        db.Modalities.AddRange(
            new Modality { Id = 1, Code = "IN_PERSON", Name = Modalities.InPerson },
            new Modality { Id = 2, Code = "VIRTUAL", Name = Modalities.Virtual });
        db.Locations.AddRange(
            new Location { Id = 1, Name = "Cubículo FCQI", ModalityId = 1 },
            new Location { Id = 2, Name = "Sala virtual", ModalityId = 2, Details = RoomLink });
        db.Subjects.Add(new Subject { Id = 1, Code = "CALCULO", Name = "Cálculo Diferencial" });

        db.People.AddRange(
            Person(Advisor, "Felipe", "felipe.marquez@uabc.edu.mx"),
            Person(Student, "Yesua", "yesua.diaz@uabc.edu.mx"),
            Person(Admin, "Directora", "direccion@uabc.edu.mx"));
        db.AdvisorProfiles.Add(new AdvisorProfile { PersonId = Advisor, ProgramId = 1, DefaultModalityId = 1 });
        db.StudentProfiles.Add(new StudentProfile { PersonId = Student, StudentNumber = "2208134" });
        db.AdvisorSubjects.Add(new AdvisorSubject { TermId = 1, AdvisorId = Advisor, SubjectId = 1 });

        db.Availabilities.AddRange(
            new Availability
            {
                Id = InPersonBlock, TermId = 1, AdvisorId = Advisor, DayOfWeek = DayOfWeek.Wednesday,
                StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(14, 0), MaxCapacity = 1, LocationId = 1
            },
            new Availability
            {
                Id = VirtualBlock, TermId = 1, AdvisorId = Advisor, DayOfWeek = DayOfWeek.Friday,
                StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(12, 0), MaxCapacity = 1, LocationId = 2
            });

        db.AdvisorySessions.Add(new AdvisorySession
        {
            Id = 1, TermId = 1, AvailabilityId = block, AdvisorId = Advisor, StudentId = Student,
            SubjectId = 1, ScheduledAt = WednesdaySession, SeatNumber = 1, StatusId = status, Topic = "Límites; series, y más"
        });

        await db.SaveChangesAsync();
        return db;
    }

    private static Person Person(int id, string first, string email)
    {
        var person = new Person { Id = id, FirstName = first, LastNamePaternal = "Prueba", Email = email };

        // DisplayName lo genera MySQL; el proveedor en memoria no lo calcula.
        typeof(Person).GetProperty(nameof(Domain.Entities.Person.DisplayName))!
            .SetValue(person, $"{first} Prueba");
        return person;
    }
}
