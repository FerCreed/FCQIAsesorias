namespace FCQI.Domain;

/// <summary>
/// Nombres de los estados, tal y como los expone la API y los muestra la
/// interfaz. Deben coincidir con session_statuses.Name en la base.
/// </summary>
public static class SessionStatuses
{
    public const string Pending = "Pendiente";
    public const string Confirmed = "Confirmada";
    public const string Cancelled = "Cancelada";
    public const string Rejected = "Rechazada";

    public static readonly IReadOnlyList<string> All =
        [Pending, Confirmed, Cancelled, Rejected];
}

/// <summary>
/// Identificadores de session_statuses. Son fijos: el esquema los inserta en
/// 01-schema.sql porque las llaves foráneas dependen de que existan.
/// </summary>
public static class SessionStatusIds
{
    public const byte Pending = 1;
    public const byte Confirmed = 2;
    public const byte Cancelled = 3;
    public const byte Rejected = 4;

    public static byte FromName(string name) => name switch
    {
        SessionStatuses.Pending => Pending,
        SessionStatuses.Confirmed => Confirmed,
        SessionStatuses.Cancelled => Cancelled,
        SessionStatuses.Rejected => Rejected,
        _ => throw new InvalidOperationException($"Estado no válido: '{name}'.")
    };
}

public static class UserRoles
{
    public const string Student = "Alumno";
    public const string Advisor = "Asesor";
    public const string Admin = "Directivo";
}

public static class Modalities
{
    public const string InPerson = "Presencial";
    public const string Virtual = "Virtual";
}
