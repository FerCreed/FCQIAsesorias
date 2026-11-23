using FCQI.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FCQI.Api.Filters;

// Traduce las excepciones de la capa de aplicación a respuestas HTTP con un
// { message } que la interfaz puede enseñar tal cual. Sustituye el
// try/catch que se repetía en cada acción de cada controlador.
public class ApiExceptionFilter : IExceptionFilter
{
    // Asigna el código HTTP según el tipo de excepción. El orden del switch
    // importa: NotFoundException hereda de
    // BusinessRuleException y debe evaluarse antes.
    public void OnException(ExceptionContext context)
    {
        var status = context.Exception switch
        {
            ForbiddenOperationException => StatusCodes.Status403Forbidden,
            NotFoundException => StatusCodes.Status404NotFound,
            BusinessRuleException => StatusCodes.Status400BadRequest,
            _ => (int?)null
        };

        // Lo inesperado (incluidas las InvalidOperationException de EF Core o
        // del framework) sigue su camino: 500 y traza en el log, sin enseñar
        // detalles internos al cliente.
        if (status is null)
        {
            return;
        }

        context.Result = new ObjectResult(new { message = context.Exception.Message }) { StatusCode = status };
        context.ExceptionHandled = true;
    }
}
