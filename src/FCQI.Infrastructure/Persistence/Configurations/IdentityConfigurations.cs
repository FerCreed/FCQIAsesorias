using FCQI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCQI.Infrastructure.Persistence.Configurations;

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("people");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Honorific).HasMaxLength(20);
        builder.Property(p => p.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.LastNamePaternal).HasMaxLength(100).IsRequired();
        builder.Property(p => p.LastNameMaternal).HasMaxLength(100);
        builder.Property(p => p.Email).HasMaxLength(150).IsRequired();

        // Columna generada por MySQL: se lee, nunca se escribe.
        builder.Property(p => p.DisplayName)
            .HasMaxLength(302)
            .ValueGeneratedOnAddOrUpdate()
            .Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);

        builder.Property(p => p.CreatedAt).ValueGeneratedOnAdd();
        builder.Property(p => p.UpdatedAt).ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(p => p.Email).IsUnique();
    }
}

public class StudentProfileConfiguration : IEntityTypeConfiguration<StudentProfile>
{
    public void Configure(EntityTypeBuilder<StudentProfile> builder)
    {
        builder.ToTable("student_profiles");
        builder.HasKey(s => s.PersonId);

        builder.Property(s => s.StudentNumber).HasMaxLength(20).IsRequired();
        builder.HasIndex(s => s.StudentNumber).IsUnique();

        builder.Property(s => s.CreatedAt).ValueGeneratedOnAdd();

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
        builder.ToTable("advisor_profiles");
        builder.HasKey(a => a.PersonId);

        builder.Property(a => a.CreatedAt).ValueGeneratedOnAdd();

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
        builder.ToTable("admin_profiles");
        builder.HasKey(a => a.PersonId);
        builder.Property(a => a.Title).HasMaxLength(200).IsRequired();

        builder.Property(a => a.CreatedAt).ValueGeneratedOnAdd();

        builder.HasOne(a => a.Person)
            .WithOne(p => p.AdminProfile)
            .HasForeignKey<AdminProfile>(a => a.PersonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
