using FCQI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCQI.Infrastructure.Persistence.Configurations;

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("personas");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasColumnName("Id");
        builder.Property(p => p.Honorific).HasColumnName("Tratamiento").HasMaxLength(20);
        builder.Property(p => p.FirstName).HasColumnName("Nombres").HasMaxLength(100).IsRequired();
        builder.Property(p => p.LastNamePaternal).HasColumnName("ApellidoPaterno").HasMaxLength(100).IsRequired();
        builder.Property(p => p.LastNameMaternal).HasColumnName("ApellidoMaterno").HasMaxLength(100);
        builder.Property(p => p.Email).HasColumnName("Correo").HasMaxLength(150).IsRequired();
        builder.Property(p => p.IsActive).HasColumnName("Activo");

        // Columna generada por MySQL: se lee, nunca se escribe.
        builder.Property(p => p.DisplayName)
            .HasColumnName("NombreCompleto")
            .HasMaxLength(302)
            .ValueGeneratedOnAddOrUpdate()
            .Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);

        builder.Property(p => p.CreatedAt).HasColumnName("CreadoEn").ValueGeneratedOnAdd();
        builder.Property(p => p.UpdatedAt).HasColumnName("ActualizadoEn").ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(p => p.Email).IsUnique();
    }
}

public class StudentProfileConfiguration : IEntityTypeConfiguration<StudentProfile>
{
    public void Configure(EntityTypeBuilder<StudentProfile> builder)
    {
        builder.ToTable("perfiles_alumno");
        builder.HasKey(s => s.PersonId);

        builder.Property(s => s.PersonId).HasColumnName("PersonaId");
        builder.Property(s => s.StudentNumber).HasColumnName("Matricula").HasMaxLength(20).IsRequired();
        builder.Property(s => s.ProgramId).HasColumnName("ProgramaId");
        builder.Property(s => s.IsActive).HasColumnName("Activo");
        builder.HasIndex(s => s.StudentNumber).IsUnique();

        builder.Property(s => s.CreatedAt).HasColumnName("CreadoEn").ValueGeneratedOnAdd();

        builder.HasOne(s => s.Person)
            .WithOne(p => p.StudentProfile)
            .HasForeignKey<StudentProfile>(s => s.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Program)
            .WithMany()
            .HasForeignKey(s => s.ProgramId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AdvisorProfileConfiguration : IEntityTypeConfiguration<AdvisorProfile>
{
    public void Configure(EntityTypeBuilder<AdvisorProfile> builder)
    {
        builder.ToTable("perfiles_asesor");
        builder.HasKey(a => a.PersonId);

        builder.Property(a => a.PersonId).HasColumnName("PersonaId");
        builder.Property(a => a.ProgramId).HasColumnName("ProgramaId");
        builder.Property(a => a.DefaultModalityId).HasColumnName("ModalidadPredeterminadaId");
        builder.Property(a => a.IsActive).HasColumnName("Activo");
        builder.Property(a => a.CreatedAt).HasColumnName("CreadoEn").ValueGeneratedOnAdd();

        builder.HasOne(a => a.Person)
            .WithOne(p => p.AdvisorProfile)
            .HasForeignKey<AdvisorProfile>(a => a.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Program)
            .WithMany()
            .HasForeignKey(a => a.ProgramId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.DefaultModality)
            .WithMany()
            .HasForeignKey(a => a.DefaultModalityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AdminProfileConfiguration : IEntityTypeConfiguration<AdminProfile>
{
    public void Configure(EntityTypeBuilder<AdminProfile> builder)
    {
        builder.ToTable("perfiles_directivo");
        builder.HasKey(a => a.PersonId);

        builder.Property(a => a.PersonId).HasColumnName("PersonaId");
        builder.Property(a => a.Title).HasColumnName("Cargo").HasMaxLength(200).IsRequired();
        builder.Property(a => a.IsActive).HasColumnName("Activo");
        builder.Property(a => a.CreatedAt).HasColumnName("CreadoEn").ValueGeneratedOnAdd();

        builder.HasOne(a => a.Person)
            .WithOne(p => p.AdminProfile)
            .HasForeignKey<AdminProfile>(a => a.PersonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
