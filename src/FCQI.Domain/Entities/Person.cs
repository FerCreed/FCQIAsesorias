namespace FCQI.Domain.Entities;

/// <summary>
/// Identidad única de una persona. El correo institucional la identifica; los
/// roles cuelgan de aquí como perfiles opcionales, de modo que un alumno puede
/// ser también asesor (los asesores pares del programa).
/// </summary>
public class Person
{
    public int Id { get; set; }
    public string? Honorific { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastNamePaternal { get; set; } = string.Empty;
    public string? LastNameMaternal { get; set; }

    /// <summary>
    /// Columna generada por la base de datos. Solo lectura.
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public StudentProfile? StudentProfile { get; set; }
    public AdvisorProfile? AdvisorProfile { get; set; }
    public AdminProfile? AdminProfile { get; set; }
}
