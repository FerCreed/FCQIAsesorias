using FCQI.Application.Interfaces;
using FCQI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Infrastructure.Persistence;

public class AppDbContext : DbContext, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Advisor> Advisors => Set<Advisor>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Availability> Availabilities => Set<Availability>();
    public DbSet<AdvisorySession> AdvisorySessions => Set<AdvisorySession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
