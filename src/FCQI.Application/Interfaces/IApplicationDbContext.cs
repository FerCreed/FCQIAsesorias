using FCQI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Subject> Subjects { get; }
    DbSet<Advisor> Advisors { get; }
    DbSet<Student> Students { get; }
    DbSet<Availability> Availabilities { get; }
    DbSet<AdvisorySession> AdvisorySessions { get; }
    DbSet<AdvisorSubject> AdvisorSubjects { get; }
    DbSet<FCQI.Domain.Entities.Admin> Admins { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
