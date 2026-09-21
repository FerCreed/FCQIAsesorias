namespace FCQI.Domain;

public static class SessionStatuses
{
    public const string Pending = "Pendiente";
    public const string Confirmed = "Confirmada";
    public const string Cancelled = "Cancelada";
    public const string Rejected = "Rechazada";
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
