using FCQI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCQI.Infrastructure.Persistence.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("students");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.Email)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(s => s.StudentNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(s => s.Email)
            .IsUnique();

        builder.HasIndex(s => s.StudentNumber)
            .IsUnique();
    }
}
