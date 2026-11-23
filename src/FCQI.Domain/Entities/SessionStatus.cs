namespace FCQI.Domain.Entities;

// Fila del catálogo de estados de asesoría. Los identificadores son fijos y
// se reflejan en SessionStatusIds.
public class SessionStatus
{
    // Llave primaria fija del estado.
    public byte Id { get; set; }

    // Clave estable del estado, usada por el código.
    public string Code { get; set; } = string.Empty;

    // Nombre que expone la API y muestra la interfaz.
    public string Name { get; set; } = string.Empty;

    // Si el estado ocupa cupo en el bloque de horario.
    public bool IsActive { get; set; }
}
