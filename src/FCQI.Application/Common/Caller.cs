namespace FCQI.Application.Common;

/// <summary>
/// Identidad de quien hace la petición, tomada del JWT. Las reglas de quién
/// puede hacer qué viven en la capa de aplicación, no en los controladores,
/// para poder probarlas sin levantar la API.
/// </summary>
public record Caller(int PersonId, bool IsAdmin, bool IsAdvisor, bool IsStudent);

/// <summary>
/// El usuario está autenticado pero no tiene permiso. La API la traduce a 403;
/// InvalidOperationException, que es un error de datos, sigue siendo 400.
/// </summary>
public class ForbiddenOperationException : Exception
{
    public ForbiddenOperationException(string message) : base(message) { }
}
