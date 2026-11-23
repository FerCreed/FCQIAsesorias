namespace FCQI.Domain.Entities;

// Catálogo de modalidades (presencial o virtual). Es tabla y no enum para
// que la base valide las llaves foráneas que lo referencian.
public class Modality
{
    // Llave primaria; byte porque el catálogo es mínimo.
    public byte Id { get; set; }

    // Clave estable de la modalidad, usada por el código.
    public string Code { get; set; } = string.Empty;

    // Nombre que se muestra en la interfaz.
    public string Name { get; set; } = string.Empty;
}
