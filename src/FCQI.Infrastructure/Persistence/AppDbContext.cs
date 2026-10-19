using FCQI.Application.Interfaces;
using FCQI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Infrastructure.Persistence;

/// <summary>
/// El esquema ya NO lo gestiona EF Core. Lo definen db/01-schema.sql y
/// db/02-seed.sql, que el contenedor ejecuta antes de arrancar la API. Por eso
/// no hay migraciones ni seeder: este contexto solo lee y escribe sobre una
/// estructura que ya existe.
/// </summary>
public class AppDbContext : DbContext, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Person> People => Set<Person>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<AdvisorProfile> AdvisorProfiles => Set<AdvisorProfile>();
    public DbSet<AdminProfile> AdminProfiles => Set<AdminProfile>();

    public DbSet<AcademicProgram> Programs => Set<AcademicProgram>();
    public DbSet<AcademicTerm> Terms => Set<AcademicTerm>();
    public DbSet<Modality> Modalities => Set<Modality>();
    public DbSet<SessionStatus> SessionStatuses => Set<SessionStatus>();
    public DbSet<Location> Locations => Set<Location>();

    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<ProgramSubject> ProgramSubjects => Set<ProgramSubject>();
    public DbSet<AdvisorSubject> AdvisorSubjects => Set<AdvisorSubject>();
    public DbSet<Availability> Availabilities => Set<Availability>();
    public DbSet<AdvisorySession> AdvisorySessions => Set<AdvisorySession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
