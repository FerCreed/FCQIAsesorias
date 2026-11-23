using FCQI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCQI.Infrastructure.Persistence.Configurations;

// Las tablas y las columnas de la base están en español (db/01-schema.sql);
// las clases del dominio conservan sus nombres en inglés. Cada propiedad
// declara su columna con HasColumnName: sin eso, la convención de EF Core
// buscaría una columna con el nombre de la propiedad y no existiría.

// Mapea AcademicProgram a la tabla programas.
public class AcademicProgramConfiguration : IEntityTypeConfiguration<AcademicProgram>
{
    // Lo invoca EF Core al construir el modelo (ApplyConfigurationsFromAssembly).
    public void Configure(EntityTypeBuilder<AcademicProgram> builder)
    {
        builder.ToTable("programas");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("Id");
        builder.Property(p => p.Code).HasColumnName("Codigo").HasMaxLength(30).IsRequired();
        builder.Property(p => p.Name).HasColumnName("Nombre").HasMaxLength(120).IsRequired();
        builder.Property(p => p.IsActive).HasColumnName("Activo");
        builder.HasIndex(p => p.Code).IsUnique();
    }
}

// Mapea AcademicTerm a la tabla ciclos_escolares.
public class AcademicTermConfiguration : IEntityTypeConfiguration<AcademicTerm>
{
    // Lo invoca EF Core al construir el modelo (ApplyConfigurationsFromAssembly).
    public void Configure(EntityTypeBuilder<AcademicTerm> builder)
    {
        builder.ToTable("ciclos_escolares");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("Id");
        builder.Property(t => t.Code).HasColumnName("Codigo").HasMaxLength(10).IsRequired();
        builder.Property(t => t.Name).HasColumnName("Nombre").HasMaxLength(60).IsRequired();
        builder.Property(t => t.StartsOn).HasColumnName("FechaInicio");
        builder.Property(t => t.EndsOn).HasColumnName("FechaFin");
        builder.Property(t => t.IsCurrent).HasColumnName("EsActual");
        builder.HasIndex(t => t.Code).IsUnique();
    }
}

// Mapea Modality a modalidades. Ids fijos (ValueGeneratedNever) porque el código los referencia.
public class ModalityConfiguration : IEntityTypeConfiguration<Modality>
{
    // Lo invoca EF Core al construir el modelo (ApplyConfigurationsFromAssembly).
    public void Configure(EntityTypeBuilder<Modality> builder)
    {
        builder.ToTable("modalidades");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(m => m.Code).HasColumnName("Codigo").HasMaxLength(20).IsRequired();
        builder.Property(m => m.Name).HasColumnName("Nombre").HasMaxLength(50).IsRequired();
        builder.HasIndex(m => m.Code).IsUnique();
    }
}

// Mapea SessionStatus a estados_sesion. Ids fijos porque SessionStatusIds los usa como constantes.
public class SessionStatusConfiguration : IEntityTypeConfiguration<SessionStatus>
{
    // Lo invoca EF Core al construir el modelo (ApplyConfigurationsFromAssembly).
    public void Configure(EntityTypeBuilder<SessionStatus> builder)
    {
        builder.ToTable("estados_sesion");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(s => s.Code).HasColumnName("Codigo").HasMaxLength(20).IsRequired();
        builder.Property(s => s.Name).HasColumnName("Nombre").HasMaxLength(50).IsRequired();
        builder.Property(s => s.IsActive).HasColumnName("Activo");
        builder.HasIndex(s => s.Code).IsUnique();
    }
}

// Mapea Location a lugares; Restrict impide borrar una modalidad en uso.
public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    // Lo invoca EF Core al construir el modelo (ApplyConfigurationsFromAssembly).
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("lugares");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("Id");
        builder.Property(l => l.Name).HasColumnName("Nombre").HasMaxLength(200).IsRequired();
        builder.Property(l => l.ModalityId).HasColumnName("ModalidadId");
        builder.Property(l => l.Details).HasColumnName("Detalles").HasMaxLength(500);
        builder.Property(l => l.IsActive).HasColumnName("Activo");
        builder.HasIndex(l => l.Name).IsUnique();

        builder.HasOne(l => l.Modality)
            .WithMany()
            .HasForeignKey(l => l.ModalityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

// Mapea Subject a materias.
public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    // Lo invoca EF Core al construir el modelo (ApplyConfigurationsFromAssembly).
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("materias");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("Id");
        builder.Property(s => s.Code).HasColumnName("Codigo").HasMaxLength(120).IsRequired();
        builder.Property(s => s.Name).HasColumnName("Nombre").HasMaxLength(200).IsRequired();
        builder.Property(s => s.IsActive).HasColumnName("Activo");
        builder.HasIndex(s => s.Code).IsUnique();
    }
}

// Mapea la relación carrera-materia; Cascade porque la fila no tiene sentido sin sus dos extremos.
public class ProgramSubjectConfiguration : IEntityTypeConfiguration<ProgramSubject>
{
    // Lo invoca EF Core al construir el modelo (ApplyConfigurationsFromAssembly).
    public void Configure(EntityTypeBuilder<ProgramSubject> builder)
    {
        builder.ToTable("programas_materias");
        builder.HasKey(ps => new { ps.ProgramId, ps.SubjectId });
        builder.Property(ps => ps.ProgramId).HasColumnName("ProgramaId");
        builder.Property(ps => ps.SubjectId).HasColumnName("MateriaId");
        builder.Property(ps => ps.Semester).HasColumnName("Semestre");

        builder.HasOne(ps => ps.Program)
            .WithMany(p => p.ProgramSubjects)
            .HasForeignKey(ps => ps.ProgramId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ps => ps.Subject)
            .WithMany(s => s.ProgramSubjects)
            .HasForeignKey(ps => ps.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
