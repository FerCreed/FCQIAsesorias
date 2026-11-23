namespace FCQI.Application.Common;

// Identidad de quien hace la petición, tomada del JWT. Las reglas de quién
// puede hacer qué viven en la capa de aplicación, no en los controladores,
// para poder probarlas sin levantar la API.
public record Caller(int PersonId, bool IsAdmin, bool IsAdvisor, bool IsStudent);

// Una regla de negocio no se cumple (datos inválidos, cupo lleno, estado
// final…). La API la traduce a 400 y muestra su mensaje. Cualquier otra
// InvalidOperationException —por ejemplo, de EF Core— es un fallo interno y
// sale como 500, sin enseñar su texto al usuario.
public class BusinessRuleException : InvalidOperationException
{
    // Crea la excepción con un mensaje apto para mostrarse al usuario.
    public BusinessRuleException(string message, Exception? inner = null) : base(message, inner) { }
}

// El usuario está autenticado pero no tiene permiso. La API la traduce a 403.
public class ForbiddenOperationException : Exception
{
    // Crea la excepción con el motivo del rechazo, que se muestra al usuario.
    public ForbiddenOperationException(string message) : base(message) { }
}

// El recurso pedido no existe. La API la traduce a 404.
public class NotFoundException : BusinessRuleException
{
    // Crea la excepción indicando qué recurso no se encontró.
    public NotFoundException(string message) : base(message) { }
}
