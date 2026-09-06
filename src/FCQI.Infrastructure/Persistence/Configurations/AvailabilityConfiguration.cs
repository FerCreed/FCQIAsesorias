using FCQI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCQI.Infrastructure.Persistence.Configurations;

public class AvailabilityConfiguration : IEntityTypeConfiguration<Availability>
{
    public void Configure(EntityTypeBuilder<Availability> builder)
    {
        builder.ToTable("availabilities");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Modality)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.Location)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasOne(a => a.Advisor)
            .WithMany(a => a.Availabilities)
            .HasForeignKey(a => a.AdvisorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
