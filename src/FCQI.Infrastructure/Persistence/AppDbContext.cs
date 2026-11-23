using FCQI.Application.Interfaces;
using FCQI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FCQI.Infrastructure.Persistence;

// El esquema ya NO lo gestiona EF Core. Lo definen db/01-schema.sql y
// db/02-seed.sql, que el contenedor ejecuta antes de arrancar la API. Por eso
// no hay migraciones ni seeder: este contexto solo lee y escribe sobre una
// estructura que ya existe.
public class AppDbContext : DbContext, IApplicationDbContext
{
    // Recibe las opciones (proveedor MySQL y cadena de conexión) desde la inyección.
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Propiedades de solo lectura con Set<T>(): evitan setters públicos y
    // advertencias de nulabilidad, y cumplen el contrato de IApplicationDbContext.
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
    public DbSet<SessionLog> SessionLogs => Set<SessionLog>();
    public DbSet<SessionStatusChange> SessionStatusChanges => Set<SessionStatusChange>();

    // Expone la transacción de EF Core a través del puerto, sin filtrar Database a la aplicación.
    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => Database.BeginTransactionAsync(cancellationToken);

    // Aplica todas las clases IEntityTypeConfiguration del ensamblado, para que
    // el mapeo de cada tabla viva en su propio archivo y no en un método gigante.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
