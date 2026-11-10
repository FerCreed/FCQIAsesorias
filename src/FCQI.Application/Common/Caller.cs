namespace FCQI.Application.Common;

/// <summary>
/// Identidad de quien hace la petición, tomada del JWT. Las reglas de quién
/// puede hacer qué viven en la capa de aplicación, no en los controladores,
/// para poder probarlas sin levantar la API.
/// </summary>
public record Caller(int PersonId, bool IsAdmin, bool IsAdvisor, bool IsStudent);

/// <summary>
/// Una regla de negocio no se cumple (datos inválidos, cupo lleno, estado
/// final…). La API la traduce a 400 y muestra su mensaje. Cualquier otra
/// InvalidOperationException —por ejemplo, de EF Core— es un fallo interno y
/// sale como 500, sin enseñar su texto al usuario.
/// </summary>
public class BusinessRuleException : InvalidOperationException
{
    public BusinessRuleException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>El usuario está autenticado pero no tiene permiso. La API la traduce a 403.</summary>
public class ForbiddenOperationException : Exception
{
    public ForbiddenOperationException(string message) : base(message) { }
}

/// <summary>El recurso pedido no existe. La API la traduce a 404.</summary>
public class NotFoundException : BusinessRuleException
{
    public NotFoundException(string message) : base(message) { }
}
