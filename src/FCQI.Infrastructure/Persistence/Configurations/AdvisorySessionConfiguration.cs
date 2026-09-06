using FCQI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCQI.Infrastructure.Persistence.Configurations;

public class AdvisorySessionConfiguration : IEntityTypeConfiguration<AdvisorySession>
{
    public void Configure(EntityTypeBuilder<AdvisorySession> builder)
    {
        builder.ToTable("advisory_sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Topic)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(s => s.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasOne(s => s.Student)
            .WithMany(s => s.AdvisorySessions)
            .HasForeignKey(s => s.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Advisor)
            .WithMany(a => a.AdvisorySessions)
            .HasForeignKey(s => s.AdvisorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Subject)
            .WithMany(s => s.AdvisorySessions)
            .HasForeignKey(s => s.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Availability)
            .WithMany(a => a.AdvisorySessions)
            .HasForeignKey(s => s.AvailabilityId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
