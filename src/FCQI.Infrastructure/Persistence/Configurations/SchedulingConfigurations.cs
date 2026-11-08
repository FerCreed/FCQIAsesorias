using FCQI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCQI.Infrastructure.Persistence.Configurations;

public class AdvisorSubjectConfiguration : IEntityTypeConfiguration<AdvisorSubject>
{
    public void Configure(EntityTypeBuilder<AdvisorSubject> builder)
    {
        builder.ToTable("asesores_materias");
        builder.HasKey(x => new { x.TermId, x.AdvisorId, x.SubjectId });

        builder.Property(x => x.TermId).HasColumnName("CicloId");
        builder.Property(x => x.AdvisorId).HasColumnName("AsesorId");
        builder.Property(x => x.SubjectId).HasColumnName("MateriaId");
        builder.Property(x => x.CreatedAt).HasColumnName("CreadoEn").ValueGeneratedOnAdd();

        builder.HasOne(x => x.Term)
            .WithMany()
            .HasForeignKey(x => x.TermId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Advisor)
            .WithMany(a => a.AdvisorSubjects)
            .HasForeignKey(x => x.AdvisorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Subject)
            .WithMany(s => s.AdvisorSubjects)
            .HasForeignKey(x => x.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AvailabilityConfiguration : IEntityTypeConfiguration<Availability>
{
    public void Configure(EntityTypeBuilder<Availability> builder)
    {
        builder.ToTable("horarios");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id).HasColumnName("Id");
        builder.Property(a => a.TermId).HasColumnName("CicloId");
        builder.Property(a => a.AdvisorId).HasColumnName("AsesorId");
        builder.Property(a => a.MaxCapacity).HasColumnName("CupoMaximo");
        builder.Property(a => a.LocationId).HasColumnName("LugarId");
        builder.Property(a => a.IsActive).HasColumnName("Activo");

        // MySQL guarda 0=domingo .. 6=sábado, igual que System.DayOfWeek.
        builder.Property(a => a.DayOfWeek).HasColumnName("DiaSemana").HasConversion<byte>();
        builder.Property(a => a.StartTime).HasColumnName("HoraInicio").HasColumnType("time");
        builder.Property(a => a.EndTime).HasColumnName("HoraFin").HasColumnType("time");
        builder.Property(a => a.CreatedAt).HasColumnName("CreadoEn").ValueGeneratedOnAdd();

        // Índice único que respalda la llave foránea compuesta de las sesiones:
        // es lo que impide que una sesión declare un asesor que no es el dueño
        // del bloque.
        builder.HasIndex(a => new { a.Id, a.AdvisorId }).IsUnique();
        builder.HasIndex(a => new { a.TermId, a.AdvisorId, a.DayOfWeek, a.StartTime }).IsUnique();

        builder.HasOne(a => a.Term)
            .WithMany()
            .HasForeignKey(a => a.TermId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Advisor)
            .WithMany(x => x.Availabilities)
            .HasForeignKey(a => a.AdvisorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Location)
            .WithMany()
            .HasForeignKey(a => a.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AdvisorySessionConfiguration : IEntityTypeConfiguration<AdvisorySession>
{
    public void Configure(EntityTypeBuilder<AdvisorySession> builder)
    {
        builder.ToTable("asesorias");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("Id");
        builder.Property(s => s.TermId).HasColumnName("CicloId");
        builder.Property(s => s.AvailabilityId).HasColumnName("HorarioId");
        builder.Property(s => s.AdvisorId).HasColumnName("AsesorId");
        builder.Property(s => s.StudentId).HasColumnName("AlumnoId");
        builder.Property(s => s.SubjectId).HasColumnName("MateriaId");
        builder.Property(s => s.ScheduledAt).HasColumnName("ProgramadaEn");
        builder.Property(s => s.SeatNumber).HasColumnName("NumeroLugar");
        builder.Property(s => s.StatusId).HasColumnName("EstadoId");
        builder.Property(s => s.Topic).HasColumnName("Tema").HasMaxLength(500).IsRequired();
        builder.Property(s => s.MeetingLink).HasColumnName("EnlaceReunion").HasMaxLength(300);
        builder.Property(s => s.CreatedAt).HasColumnName("CreadoEn").ValueGeneratedOnAdd();
        builder.Property(s => s.UpdatedAt).HasColumnName("ActualizadoEn").ValueGeneratedOnAddOrUpdate();

        // La mantiene un trigger de la base; la aplicación nunca la escribe.
        builder.Property(s => s.ActiveAt)
            .HasColumnName("ActivaEn")
            .ValueGeneratedOnAddOrUpdate()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        builder.HasOne(s => s.Term)
            .WithMany()
            .HasForeignKey(s => s.TermId)
            .OnDelete(DeleteBehavior.Restrict);

        // Llave foránea compuesta (HorarioId, AsesorId): garantiza que el
        // asesor de la sesión sea el dueño del bloque de horario.
        builder.HasOne(s => s.Availability)
            .WithMany(a => a.AdvisorySessions)
            .HasForeignKey(s => new { s.AvailabilityId, s.AdvisorId })
            .HasPrincipalKey(a => new { a.Id, a.AdvisorId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Student)
            .WithMany(x => x.AdvisorySessions)
            .HasForeignKey(s => s.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Status)
            .WithMany()
            .HasForeignKey(s => s.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        // AsesorId y MateriaId ya viajan en llaves foráneas compuestas hacia
        // horarios y asesores_materias. Estas navegaciones son solo de
        // lectura: sin ellas habría que hacer JOIN a mano en cada consulta.
        builder.HasOne(s => s.Advisor)
            .WithMany(a => a.AdvisorySessions)
            .HasForeignKey(s => s.AdvisorId)
            .OnDelete(DeleteBehavior.Restrict)
            .Metadata.IsRequired = true;

        builder.HasOne(s => s.Subject)
            .WithMany()
            .HasForeignKey(s => s.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SessionLogConfiguration : IEntityTypeConfiguration<SessionLog>
{
    public void Configure(EntityTypeBuilder<SessionLog> builder)
    {
        builder.ToTable("bitacoras_asesoria");
        builder.HasKey(l => l.SessionId);

        builder.Property(l => l.SessionId).HasColumnName("AsesoriaId");
        builder.Property(l => l.Attended).HasColumnName("Asistio");
        builder.Property(l => l.Notes).HasColumnName("Notas").HasMaxLength(2000);
        builder.Property(l => l.RecordedBy).HasColumnName("RegistradoPor");
        builder.Property(l => l.RecordedAt).HasColumnName("RegistradoEn");

        builder.HasOne(l => l.Session)
            .WithOne(s => s.Log)
            .HasForeignKey<SessionLog>(l => l.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>Solo lectura: las filas las insertan los triggers de asesorias.</summary>
public class SessionStatusChangeConfiguration : IEntityTypeConfiguration<SessionStatusChange>
{
    public void Configure(EntityTypeBuilder<SessionStatusChange> builder)
    {
        builder.ToTable("historial_estados_sesion");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.SessionId).HasColumnName("AsesoriaId");
        builder.Property(h => h.FromStatusId).HasColumnName("EstadoAnteriorId");
        builder.Property(h => h.ToStatusId).HasColumnName("EstadoNuevoId");
        builder.Property(h => h.ChangedBy).HasColumnName("CambiadoPor");
        builder.Property(h => h.ChangedAt).HasColumnName("CambiadoEn").ValueGeneratedOnAdd();
        builder.Property(h => h.Reason).HasColumnName("Motivo").HasMaxLength(500);

        builder.HasOne(h => h.Session).WithMany(s => s.History).HasForeignKey(h => h.SessionId);
        builder.HasOne(h => h.FromStatus).WithMany().HasForeignKey(h => h.FromStatusId);
        builder.HasOne(h => h.ToStatus).WithMany().HasForeignKey(h => h.ToStatusId);
    }
}
