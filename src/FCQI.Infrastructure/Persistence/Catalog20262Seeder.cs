using FCQI.Domain;
using FCQI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Infrastructure.Persistence;

public static class Catalog20262Seeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Advisors.AnyAsync(cancellationToken))
        {
            return;
        }

        db.AdvisorySessions.RemoveRange(db.AdvisorySessions);
        db.Availabilities.RemoveRange(db.Availabilities);
        db.AdvisorSubjects.RemoveRange(db.AdvisorSubjects);
        db.Students.RemoveRange(db.Students);
        db.Admins.RemoveRange(db.Admins);
        db.Advisors.RemoveRange(db.Advisors);
        db.Subjects.RemoveRange(db.Subjects);
        await db.SaveChangesAsync(cancellationToken);

        var subjects = EnsureSubjects(db);
        await db.SaveChangesAsync(cancellationToken);

        var presencial = Modalities.InPerson;
        var virtualMod = Modalities.Virtual;
        var cubicle = "Cubículo FCQI";
        var meet = "Enlace virtual (Meet/Teams)";

        AddAdvisor(db, subjects, "Felipe de Jesús Márquez Vizcarra", "felipe.marquez63@uabc.edu.mx",
            "Ingeniería en Electrónica", presencial, cubicle,
            ["Introducción a las Matemáticas Universitarias", "Cálculo Diferencial", "Cálculo Integral",
                "Circuitos de Corriente Directa", "Circuitos de Corriente Alterna", "Modelado y Control"],
            [
                (Days(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Thursday), "12:00", "16:00"),
                (Days(DayOfWeek.Tuesday), "13:00", "17:00"),
                (Days(DayOfWeek.Friday), "10:00", "13:00"),
                (Days(DayOfWeek.Friday), "15:00", "16:00")
            ]);

        AddAdvisor(db, subjects, "Edgar Kenichi Tsuchiya Godínez", "edgar.tsuchiya@uabc.edu.mx",
            "Ingeniería en Electrónica", presencial, cubicle,
            ["Introducción a las Matemáticas Universitarias", "Ecuaciones Diferenciales", "Cálculo Integral",
                "Circuitos de Corriente Directa"],
            [
                (Days(DayOfWeek.Monday, DayOfWeek.Tuesday), "08:00", "10:00"),
                (Days(DayOfWeek.Monday, DayOfWeek.Tuesday), "13:00", "15:00"),
                (Days(DayOfWeek.Wednesday), "11:00", "15:00"),
                (Days(DayOfWeek.Thursday), "08:00", "10:00"),
                (Days(DayOfWeek.Thursday), "14:00", "16:00"),
                (Days(DayOfWeek.Friday), "14:00", "18:00")
            ]);

        AddAdvisor(db, subjects, "Eduardo Isaías Mérida Rodríguez", "eduardo.merida@uabc.edu.mx",
            "Ingeniería Industrial", presencial, cubicle,
            ["Cálculo Diferencial", "Cálculo Integral", "Ecuaciones Diferenciales",
                "Introducción a las Matemáticas Universitarias", "Ingeniería Económica", "Circuitos Eléctricos"],
            [
                (Days(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday), "09:00", "13:00"),
                (Days(DayOfWeek.Friday), "12:00", "14:00"),
                (Days(DayOfWeek.Friday), "16:00", "18:00")
            ]);

        AddAdvisor(db, subjects, "Luis Alejandro Flores Díaz", "alejandro.flores48@uabc.edu.mx",
            "Ingeniería Industrial", virtualMod, meet,
            ["Inglés I", "Inglés II", "Introducción a las Matemáticas Universitarias",
                "Probabilidad y Estadística", "Investigación de Operaciones I"],
            [
                (Days(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday),
                    "17:00", "21:00")
            ]);

        AddAdvisor(db, subjects, "Carlos Enrique Martínez de la Cruz", "carlos.martinez86@uabc.edu.mx",
            "Ingeniería Industrial", presencial, cubicle,
            ["Cálculo Diferencial", "Introducción a las Matemáticas Universitarias", "Álgebra Superior"],
            [
                (Days(DayOfWeek.Monday), "12:00", "14:00"),
                (Days(DayOfWeek.Monday), "15:00", "16:00"),
                (Days(DayOfWeek.Tuesday), "10:00", "11:00"),
                (Days(DayOfWeek.Tuesday), "15:00", "16:00"),
                (Days(DayOfWeek.Wednesday), "14:00", "17:00"),
                (Days(DayOfWeek.Thursday), "10:00", "14:00"),
                (Days(DayOfWeek.Thursday), "16:00", "17:00"),
                (Days(DayOfWeek.Friday), "11:00", "16:00")
            ]);

        AddAdvisor(db, subjects, "Dylan Josué Guerrero Luque", "dylan.guerrero@uabc.edu.mx",
            "Ingeniería Química", presencial, cubicle,
            ["Cálculo Diferencial", "Cálculo Integral", "Álgebra Superior", "Balance de Materia y Energía",
                "Cinética Química y Catálisis", "Reactores Homogéneos y Heterogéneos"],
            [
                (Days(DayOfWeek.Monday), "11:00", "14:00"),
                (Days(DayOfWeek.Monday), "17:00", "18:00"),
                (Days(DayOfWeek.Tuesday, DayOfWeek.Thursday), "11:00", "14:00"),
                (Days(DayOfWeek.Wednesday), "09:00", "10:00"),
                (Days(DayOfWeek.Wednesday), "11:00", "14:00"),
                (Days(DayOfWeek.Wednesday), "15:00", "16:00"),
                (Days(DayOfWeek.Friday), "10:00", "15:00")
            ]);

        AddAdvisor(db, subjects, "Luis Alberto González Rodríguez", "luis.gonzalez.rodriguez@uabc.edu.mx",
            "Ingeniería Química", presencial, cubicle,
            ["Cálculo Integral", "Inglés I", "Inglés II", "Química", "Ecuaciones Diferenciales",
                "Electricidad y Magnetismo", "Termodinámica", "Cinética Química y Catálisis",
                "Control e Instrumentación de Procesos", "Operaciones de Separación"],
            [
                (Days(DayOfWeek.Monday, DayOfWeek.Tuesday), "09:00", "10:00"),
                (Days(DayOfWeek.Monday, DayOfWeek.Tuesday), "13:00", "15:00"),
                (Days(DayOfWeek.Monday, DayOfWeek.Tuesday), "17:00", "18:00"),
                (Days(DayOfWeek.Wednesday, DayOfWeek.Thursday), "08:00", "10:00"),
                (Days(DayOfWeek.Wednesday, DayOfWeek.Thursday), "14:00", "16:00"),
                (Days(DayOfWeek.Friday), "14:00", "18:00")
            ]);

        AddAdvisor(db, subjects, "Getzamani Guadalupe Solis Gutiérrez", "getzamani.solis@uabc.edu.mx",
            "Ingeniería Química", presencial, cubicle,
            ["Cálculo Diferencial", "Álgebra Superior", "Ecuaciones Diferenciales",
                "Balance de Materia y Energía", "Cinética Química y Catálisis"],
            [
                (Days(DayOfWeek.Monday, DayOfWeek.Wednesday), "09:00", "10:00"),
                (Days(DayOfWeek.Monday, DayOfWeek.Wednesday), "15:00", "18:00"),
                (Days(DayOfWeek.Tuesday), "07:00", "10:00"),
                (Days(DayOfWeek.Thursday), "14:00", "18:00"),
                (Days(DayOfWeek.Friday), "08:00", "11:00"),
                (Days(DayOfWeek.Friday), "14:00", "16:00")
            ]);

        AddAdvisor(db, subjects, "Mia Italia Alexandres Carrasco", "mia.alexandres@uabc.edu.mx",
            "Ingeniería Química", presencial, cubicle,
            ["Cálculo Integral", "Introducción a las Matemáticas Universitarias", "Inglés I", "Inglés II",
                "Ecuaciones Diferenciales", "Cinética Química y Catálisis",
                "Operaciones de Transferencia de Calor", "Reactores Homogéneos y Heterogéneos"],
            [
                (Days(DayOfWeek.Monday), "11:00", "14:00"),
                (Days(DayOfWeek.Tuesday, DayOfWeek.Thursday), "10:00", "15:00"),
                (Days(DayOfWeek.Wednesday), "10:00", "14:00"),
                (Days(DayOfWeek.Friday), "15:00", "18:00")
            ]);

        AddAdvisor(db, subjects, "Vladimir Bonifacio Ramírez Martínez", "v1299027@uabc.edu.mx",
            "Químico Industrial", presencial, cubicle,
            ["Química", "Química Orgánica I", "Química General", "Química Orgánica II", "Bioquímica"],
            [
                (Days(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday), "08:00", "12:00"),
                (Days(DayOfWeek.Thursday), "09:00", "12:00"),
                (Days(DayOfWeek.Thursday), "13:00", "15:00"),
                (Days(DayOfWeek.Friday), "07:00", "10:00")
            ]);

        AddAdvisor(db, subjects, "Bryan Emilio Rafael Muñoz Campos", "bryan.munoz@uabc.edu.mx",
            "Química Farmacéutica Biológica", presencial, cubicle,
            ["Química", "Matemáticas Básicas", "Química Analítica I", "Biología", "Biología Molecular",
                "Bioquímica Clínica", "Bioquímica Estructural", "Bioquímica Metabólica", "Farmacocinética"],
            [
                (Days(DayOfWeek.Monday), "09:00", "10:00"),
                (Days(DayOfWeek.Monday), "12:00", "13:00"),
                (Days(DayOfWeek.Monday), "14:00", "16:00"),
                (Days(DayOfWeek.Tuesday), "09:00", "10:00"),
                (Days(DayOfWeek.Tuesday), "14:00", "15:00"),
                (Days(DayOfWeek.Tuesday), "16:00", "18:00"),
                (Days(DayOfWeek.Wednesday), "14:00", "15:00"),
                (Days(DayOfWeek.Wednesday), "16:00", "18:00"),
                (Days(DayOfWeek.Thursday), "14:00", "18:00"),
                (Days(DayOfWeek.Friday), "13:00", "18:00")
            ]);

        AddAdvisor(db, subjects, "Jimena Beltrán Zepeda", "j2207105@uabc.edu.mx",
            "Química Farmacéutica Biológica", presencial, cubicle,
            ["Química", "Física", "Química Analítica I", "Biología", "Biofarmacia", "Farmacología",
                "Biología Celular", "Análisis Instrumental I"],
            [
                (Days(DayOfWeek.Monday, DayOfWeek.Tuesday), "08:00", "13:00"),
                (Days(DayOfWeek.Wednesday), "12:00", "16:00"),
                (Days(DayOfWeek.Thursday), "14:00", "15:00"),
                (Days(DayOfWeek.Friday), "11:00", "16:00")
            ]);

        AddAdvisor(db, subjects, "Andrés Bautista Cruz", "andres.cruz36@uabc.edu.mx",
            "Química Farmacéutica Biológica", presencial, cubicle,
            ["Química", "Biología", "Química Analítica II", "Inmunología", "Anatomía y Fisiología",
                "Farmacocinética", "Análisis Instrumental I"],
            [
                (Days(DayOfWeek.Monday), "09:00", "10:00"),
                (Days(DayOfWeek.Monday), "11:00", "13:00"),
                (Days(DayOfWeek.Monday), "15:00", "17:00"),
                (Days(DayOfWeek.Tuesday), "10:00", "15:00"),
                (Days(DayOfWeek.Wednesday), "12:00", "15:00"),
                (Days(DayOfWeek.Wednesday), "16:00", "18:00"),
                (Days(DayOfWeek.Friday), "10:00", "12:00"),
                (Days(DayOfWeek.Friday), "13:00", "16:00")
            ]);

        AddAdvisor(db, subjects, "Braulio Reynaldo Angulo Curiel", "angulo.braulio@uabc.edu.mx",
            "Química Farmacéutica Biológica", presencial, cubicle,
            ["Química", "Física", "Matemáticas Básicas", "Matemáticas Avanzadas", "Biología",
                "Bioquímica Estructural", "Biología Celular", "Bioquímica Metabólica", "Biología Molecular",
                "Inmunología"],
            [
                (Days(DayOfWeek.Monday), "09:00", "10:00"),
                (Days(DayOfWeek.Monday), "12:00", "13:00"),
                (Days(DayOfWeek.Monday), "16:00", "18:00"),
                (Days(DayOfWeek.Tuesday), "09:00", "11:00"),
                (Days(DayOfWeek.Tuesday), "13:00", "14:00"),
                (Days(DayOfWeek.Tuesday), "16:00", "18:00"),
                (Days(DayOfWeek.Wednesday), "07:00", "08:00"),
                (Days(DayOfWeek.Wednesday), "16:00", "18:00"),
                (Days(DayOfWeek.Thursday), "15:00", "18:00"),
                (Days(DayOfWeek.Friday), "13:00", "18:00")
            ]);

        db.Admins.Add(new Admin
        {
            FullName = "Dra. Lizeth Carolina Aguilar Dodier",
            Email = "progasesorias.fcqi@uabc.edu.mx",
            Title = "Responsable del Programa de Asesorías Académicas"
        });

        db.Students.AddRange(
            new Student { FullName = "Yesua Fernando Díaz Hernández", Email = "yesua.diaz@uabc.edu.mx", StudentNumber = "2208134" },
            new Student { FullName = "Juan Carlos Laguna Hernández", Email = "juan.laguna@uabc.edu.mx", StudentNumber = "2208686" },
            new Student { FullName = "Emily Zaray Romero Estrada", Email = "emily.romero@uabc.edu.mx", StudentNumber = "2209578" },
            new Student { FullName = "Carlos Ariel Ureta Armenta", Email = "carlos.ureta@uabc.edu.mx", StudentNumber = "2208511" },
            new Student { FullName = "Ana Sofía Navarro López", Email = "ana.navarro@uabc.edu.mx", StudentNumber = "2211001" },
            new Student { FullName = "Diego Armando Pérez Ruiz", Email = "diego.perez@uabc.edu.mx", StudentNumber = "2211002" });

        await db.SaveChangesAsync(cancellationToken);

        var yesua = await db.Students.SingleAsync(s => s.StudentNumber == "2208134", cancellationToken);
        var emily = await db.Students.SingleAsync(s => s.StudentNumber == "2209578", cancellationToken);
        var marquez = await db.Advisors.SingleAsync(a => a.Email == "felipe.marquez63@uabc.edu.mx", cancellationToken);
        var flores = await db.Advisors.SingleAsync(a => a.Email == "alejandro.flores48@uabc.edu.mx", cancellationToken);
        var calculo = subjects["Cálculo Diferencial"];
        var ingles = subjects["Inglés I"];
        var slotMarquez = await db.Availabilities.FirstAsync(a => a.AdvisorId == marquez.Id && a.DayOfWeek == DayOfWeek.Monday, cancellationToken);
        var slotFlores = await db.Availabilities.FirstAsync(a => a.AdvisorId == flores.Id, cancellationToken);

        var nextMonday = Next(DayOfWeek.Monday).Add(slotMarquez.StartTime.ToTimeSpan());
        db.AdvisorySessions.AddRange(
            new AdvisorySession
            {
                StudentId = yesua.Id,
                AdvisorId = marquez.Id,
                SubjectId = calculo.Id,
                AvailabilityId = slotMarquez.Id,
                ScheduledAt = nextMonday,
                Topic = "Límites y continuidad — dudas del parcial 1",
                Status = SessionStatuses.Pending
            },
            new AdvisorySession
            {
                StudentId = emily.Id,
                AdvisorId = marquez.Id,
                SubjectId = calculo.Id,
                AvailabilityId = slotMarquez.Id,
                ScheduledAt = nextMonday.AddDays(7),
                Topic = "Derivadas de funciones trigonométricas",
                Status = SessionStatuses.Pending
            },
            new AdvisorySession
            {
                StudentId = yesua.Id,
                AdvisorId = flores.Id,
                SubjectId = ingles.Id,
                AvailabilityId = slotFlores.Id,
                ScheduledAt = Next(DayOfWeek.Wednesday).AddHours(17),
                Topic = "Presente perfecto",
                Status = SessionStatuses.Confirmed
            },
            new AdvisorySession
            {
                StudentId = emily.Id,
                AdvisorId = flores.Id,
                SubjectId = ingles.Id,
                AvailabilityId = slotFlores.Id,
                ScheduledAt = Next(DayOfWeek.Thursday).AddHours(18),
                Topic = "Ensayo cancelado",
                Status = SessionStatuses.Cancelled
            });

        await db.SaveChangesAsync(cancellationToken);
    }

    private static Dictionary<string, Subject> EnsureSubjects(AppDbContext db)
    {
        string[] names =
        [
            "Introducción a las Matemáticas Universitarias", "Cálculo Diferencial", "Cálculo Integral",
            "Circuitos de Corriente Directa", "Circuitos de Corriente Alterna", "Modelado y Control",
            "Ecuaciones Diferenciales", "Ingeniería Económica", "Circuitos Eléctricos", "Inglés I", "Inglés II",
            "Probabilidad y Estadística", "Investigación de Operaciones I", "Álgebra Superior",
            "Balance de Materia y Energía", "Cinética Química y Catálisis", "Reactores Homogéneos y Heterogéneos",
            "Química", "Electricidad y Magnetismo", "Termodinámica", "Control e Instrumentación de Procesos",
            "Operaciones de Separación", "Operaciones de Transferencia de Calor", "Química Orgánica I",
            "Química General", "Química Orgánica II", "Bioquímica", "Matemáticas Básicas", "Química Analítica I",
            "Biología", "Biología Molecular", "Bioquímica Clínica", "Bioquímica Estructural",
            "Bioquímica Metabólica", "Farmacocinética", "Física", "Biofarmacia", "Farmacología",
            "Biología Celular", "Análisis Instrumental I", "Química Analítica II", "Inmunología",
            "Anatomía y Fisiología", "Matemáticas Avanzadas"
        ];

        var map = new Dictionary<string, Subject>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
        {
            var subject = new Subject
            {
                Code = Slug(name),
                Name = name,
                Program = "FCQI 2026-2"
            };
            db.Subjects.Add(subject);
            map[name] = subject;
        }

        return map;
    }

    private static void AddAdvisor(
        AppDbContext db,
        Dictionary<string, Subject> subjects,
        string name,
        string email,
        string area,
        string modality,
        string location,
        string[] subjectNames,
        (DayOfWeek[] Days, string Start, string End)[] blocks)
    {
        var advisor = new Advisor
        {
            FullName = name,
            Email = email,
            Area = area,
            DefaultModality = modality,
            IsActive = true
        };

        foreach (var subjectName in subjectNames)
        {
            advisor.AdvisorSubjects.Add(new AdvisorSubject { Subject = subjects[subjectName] });
        }

        foreach (var (days, start, end) in blocks)
        {
            foreach (var day in days)
            {
                advisor.Availabilities.Add(new Availability
                {
                    DayOfWeek = day,
                    StartTime = TimeOnly.Parse(start),
                    EndTime = TimeOnly.Parse(end),
                    MaxCapacity = 1,
                    Modality = modality,
                    Location = location
                });
            }
        }

        db.Advisors.Add(advisor);
    }

    private static DayOfWeek[] Days(params DayOfWeek[] days) => days;

    private static DateTime Next(DayOfWeek day)
    {
        var date = DateTime.Today.AddDays(1);
        while (date.DayOfWeek != day)
        {
            date = date.AddDays(1);
        }

        return date;
    }

    private static string Slug(string name)
    {
        var normalized = name.ToUpperInvariant()
            .Replace('Á', 'A').Replace('É', 'E').Replace('Í', 'I').Replace('Ó', 'O').Replace('Ú', 'U').Replace('Ñ', 'N');
        var chars = normalized.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var slug = new string(chars);
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return slug.Trim('-');
    }
}
