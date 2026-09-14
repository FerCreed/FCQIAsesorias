using FCQI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCQI.Infrastructure.Persistence.Configurations;

public class AdvisorSubjectConfiguration : IEntityTypeConfiguration<AdvisorSubject>
{
    public void Configure(EntityTypeBuilder<AdvisorSubject> builder)
    {
        builder.ToTable("advisor_subjects");

        builder.HasKey(x => new { x.AdvisorId, x.SubjectId });

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
