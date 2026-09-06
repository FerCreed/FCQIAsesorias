using FCQI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCQI.Infrastructure.Persistence.Configurations;

public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("subjects");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Code)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.Program)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(s => s.Code)
            .IsUnique();

        builder.HasData(
            new Subject { Id = 1, Code = "MAT101", Name = "Matemáticas I", Program = "Tronco Común" },
            new Subject { Id = 2, Code = "FIS101", Name = "Física I", Program = "Tronco Común" },
            new Subject { Id = 3, Code = "QUI201", Name = "Química Orgánica", Program = "Química" },
            new Subject { Id = 4, Code = "ING301", Name = "Termodinámica", Program = "Ingeniería Química" },
            new Subject { Id = 5, Code = "CAL101", Name = "Cálculo Diferencial", Program = "Tronco Común" });
    }
}
