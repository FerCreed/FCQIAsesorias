using FCQI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCQI.Infrastructure.Persistence.Configurations;

public class AcademicProgramConfiguration : IEntityTypeConfiguration<AcademicProgram>
{
    public void Configure(EntityTypeBuilder<AcademicProgram> builder)
    {
        builder.ToTable("programs");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Code).HasMaxLength(30).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(120).IsRequired();
        builder.HasIndex(p => p.Code).IsUnique();
    }
}

public class AcademicTermConfiguration : IEntityTypeConfiguration<AcademicTerm>
{
    public void Configure(EntityTypeBuilder<AcademicTerm> builder)
    {
        builder.ToTable("academic_terms");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Code).HasMaxLength(10).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(60).IsRequired();
        builder.HasIndex(t => t.Code).IsUnique();
    }
}

public class ModalityConfiguration : IEntityTypeConfiguration<Modality>
{
    public void Configure(EntityTypeBuilder<Modality> builder)
    {
        builder.ToTable("modalities");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Code).HasMaxLength(20).IsRequired();
        builder.Property(m => m.Name).HasMaxLength(50).IsRequired();
        builder.HasIndex(m => m.Code).IsUnique();
    }
}

public class SessionStatusConfiguration : IEntityTypeConfiguration<SessionStatus>
{
    public void Configure(EntityTypeBuilder<SessionStatus> builder)
    {
        builder.ToTable("session_statuses");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Code).HasMaxLength(20).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(50).IsRequired();
        builder.HasIndex(s => s.Code).IsUnique();
    }
}

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("locations");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Name).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Details).HasMaxLength(500);
        builder.HasIndex(l => l.Name).IsUnique();

        builder.HasOne(l => l.Modality)
            .WithMany()
            .HasForeignKey(l => l.ModalityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("subjects");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Code).HasMaxLength(120).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(s => s.Code).IsUnique();
    }
}

public class ProgramSubjectConfiguration : IEntityTypeConfiguration<ProgramSubject>
{
    public void Configure(EntityTypeBuilder<ProgramSubject> builder)
    {
        builder.ToTable("program_subjects");
        builder.HasKey(ps => new { ps.ProgramId, ps.SubjectId });

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
