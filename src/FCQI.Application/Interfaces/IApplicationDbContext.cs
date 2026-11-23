using FCQI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FCQI.Application.Interfaces;

// Puerto de persistencia que usan los casos de uso. Expone solo los conjuntos
// y operaciones que la aplicación necesita, de modo que la capa de aplicación
// no depende del AppDbContext concreto ni del proveedor MySQL, y en las
// pruebas puede sustituirse por un contexto en memoria.
public interface IApplicationDbContext
{
    // Identidad y perfiles por rol.
    DbSet<Person> People { get; }
    DbSet<StudentProfile> StudentProfiles { get; }
    DbSet<AdvisorProfile> AdvisorProfiles { get; }
    DbSet<AdminProfile> AdminProfiles { get; }

    // Catálogos casi estáticos.
    DbSet<AcademicProgram> Programs { get; }
    DbSet<AcademicTerm> Terms { get; }
    DbSet<Modality> Modalities { get; }
    DbSet<SessionStatus> SessionStatuses { get; }
    DbSet<Location> Locations { get; }

    // Materias, horarios y asesorías: los datos operativos del programa.
    DbSet<Subject> Subjects { get; }
    DbSet<ProgramSubject> ProgramSubjects { get; }
    DbSet<AdvisorSubject> AdvisorSubjects { get; }
    DbSet<Availability> Availabilities { get; }
    DbSet<AdvisorySession> AdvisorySessions { get; }
    DbSet<SessionLog> SessionLogs { get; }
    DbSet<SessionStatusChange> SessionStatusChanges { get; }

    // Persiste los cambios pendientes y devuelve el número de filas afectadas.
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Para casos de uso que escriben varias veces y deben quedar todo o nada.
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
