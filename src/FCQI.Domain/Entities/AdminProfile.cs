namespace FCQI.Domain.Entities;

// Perfil de directivo de una persona. Es opcional y cuelga de
// Person para que los roles se sumen sin duplicar identidades.
public class AdminProfile
{
    // Llave primaria y foránea a la persona: un perfil de directivo por persona.
    public int PersonId { get; set; }

    // Cargo del directivo, mostrado en reportes y correos.
    public string Title { get; set; } = string.Empty;

    // Baja lógica: revoca el acceso de dirección sin borrar la persona.
    public bool IsActive { get; set; } = true;

    // Fecha de alta del perfil, en UTC.
    public DateTime CreatedAt { get; set; }

    // Persona dueña del perfil.
    public Person Person { get; set; } = null!;
}
