using FCQI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCQI.Infrastructure.Persistence.Configurations;

public class AdvisorConfiguration : IEntityTypeConfiguration<Advisor>
{
    public void Configure(EntityTypeBuilder<Advisor> builder)
    {
        builder.ToTable("advisors");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.Email)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(a => a.Area)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(a => a.TutorKind)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(a => a.DefaultModality)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(a => a.Email)
            .IsUnique();
    }
}
