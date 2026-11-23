namespace FCQI.Domain.Entities;

// Identidad única de una persona. El correo institucional la identifica; los
// roles cuelgan de aquí como perfiles opcionales, de modo que un alumno puede
// ser también asesor (los asesores pares del programa).
public class Person
{
    // Llave primaria de la persona; la comparten todos sus perfiles.
    public int Id { get; set; }

    // Tratamiento opcional (Dr., Mtra.) antepuesto al nombre visible.
    public string? Honorific { get; set; }

    // Nombre o nombres de pila.
    public string FirstName { get; set; } = string.Empty;

    // Apellido paterno, obligatorio.
    public string LastNamePaternal { get; set; } = string.Empty;

    // Apellido materno; opcional porque no todas las personas lo tienen.
    public string? LastNameMaternal { get; set; }

    // Columna generada por la base de datos. Solo lectura.
    public string DisplayName { get; private set; } = string.Empty;

    // Correo institucional; identifica a la persona al iniciar sesión.
    public string Email { get; set; } = string.Empty;

    // Baja lógica: bloquea el acceso conservando el historial.
    public bool IsActive { get; set; } = true;

    // Fecha de alta, en UTC.
    public DateTime CreatedAt { get; set; }

    // Fecha de la última modificación, en UTC.
    public DateTime UpdatedAt { get; set; }

    // Perfil de alumno, si la persona lo tiene.
    public StudentProfile? StudentProfile { get; set; }

    // Perfil de asesor, si la persona lo tiene.
    public AdvisorProfile? AdvisorProfile { get; set; }

    // Perfil de directivo, si la persona lo tiene.
    public AdminProfile? AdminProfile { get; set; }
}
