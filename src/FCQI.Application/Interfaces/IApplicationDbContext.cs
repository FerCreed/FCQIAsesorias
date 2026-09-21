using FCQI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Person> People { get; }
    DbSet<StudentProfile> StudentProfiles { get; }
    DbSet<AdvisorProfile> AdvisorProfiles { get; }
    DbSet<AdminProfile> AdminProfiles { get; }

    DbSet<AcademicProgram> Programs { get; }
    DbSet<AcademicTerm> Terms { get; }
    DbSet<Modality> Modalities { get; }
    DbSet<SessionStatus> SessionStatuses { get; }
    DbSet<Location> Locations { get; }

    DbSet<Subject> Subjects { get; }
    DbSet<ProgramSubject> ProgramSubjects { get; }
    DbSet<AdvisorSubject> AdvisorSubjects { get; }
    DbSet<Availability> Availabilities { get; }
    DbSet<AdvisorySession> AdvisorySessions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
