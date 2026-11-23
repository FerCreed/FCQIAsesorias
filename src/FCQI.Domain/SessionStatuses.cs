namespace FCQI.Domain;

// Nombres de los estados, tal y como los expone la API y los muestra la
// interfaz. Deben coincidir con estados_sesion.Nombre en la base.
public static class SessionStatuses
{
    // Solicitada por el alumno, a la espera del asesor.
    public const string Pending = "Pendiente";

    // Aceptada por el asesor; ocupa cupo.
    public const string Confirmed = "Confirmada";

    // Retirada por el alumno o el asesor; libera el cupo.
    public const string Cancelled = "Cancelada";

    // Declinada por el asesor; libera el cupo.
    public const string Rejected = "Rechazada";

    // Impartida y con bitácora registrada.
    public const string Completed = "Realizada";

    // Todos los estados en el orden del ciclo de vida, para filtros y reportes.
    public static readonly IReadOnlyList<string> All =
        [Pending, Confirmed, Cancelled, Rejected, Completed];
}

// Identificadores de estados_sesion. Son fijos: el esquema los inserta en
// 01-schema.sql porque las llaves foráneas dependen de que existan.
public static class SessionStatusIds
{
    // Identificador de Pending.
    public const byte Pending = 1;

    // Identificador de Confirmed.
    public const byte Confirmed = 2;

    // Identificador de Cancelled.
    public const byte Cancelled = 3;

    // Identificador de Rejected.
    public const byte Rejected = 4;

    // Identificador de Completed.
    public const byte Completed = 5;

    // Traduce el nombre que llega por la API a su identificador. Lanza en vez
    // de devolver un valor por omisión para que un estado inválido nunca se
    // guarde en silencio.
    public static byte FromName(string name) => name switch
    {
        SessionStatuses.Pending => Pending,
        SessionStatuses.Confirmed => Confirmed,
        SessionStatuses.Cancelled => Cancelled,
        SessionStatuses.Rejected => Rejected,
        SessionStatuses.Completed => Completed,
        _ => throw new InvalidOperationException($"Estado no válido: '{name}'.")
    };
}

// Nombres de los roles que viajan en el token y en la API. Centralizados para
// que las políticas de autorización y la interfaz usen el mismo texto.
public static class UserRoles
{
    // Rol de alumno: solicita asesorías.
    public const string Student = "Alumno";

    // Rol de asesor: publica horarios y atiende asesorías.
    public const string Advisor = "Asesor";

    // Rol de dirección: administra asesores y consulta reportes.
    public const string Admin = "Directivo";
}

// Nombres de las modalidades tal como los expone la API.
public static class Modalities
{
    // Asesoría en una sede física.
    public const string InPerson = "Presencial";

    // Asesoría por videollamada.
    public const string Virtual = "Virtual";
}

// Tipos de asesor que distingue el catálogo.
public static class AdvisorKinds
{
    // Asesor docente.
    public const string Professor = "Profesor";

    // Asesor par: un alumno que también tiene perfil de asesor.
    public const string Student = "Estudiante";
}
