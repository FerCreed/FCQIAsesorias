using FCQI.Application.Subjects.Dtos;
using FCQI.Application.Subjects.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCQI.Api.Controllers;

// Catálogo de materias del ciclo vigente.
[ApiController]
[Route("api/[controller]")]
// Catálogo del programa: cualquier usuario autenticado puede consultarlo.
[Authorize]
public class SubjectsController : ControllerBase
{
    private readonly GetSubjectsQueryHandler _handler;

    // Recibe el caso de uso por constructor porque el controlador solo tiene una acción.
    public SubjectsController(GetSubjectsQueryHandler handler)
    {
        _handler = handler;
    }

    // Materias activas; anónimo para que el catálogo se vea antes de iniciar sesión.
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<SubjectDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SubjectDto>>> Get(CancellationToken cancellationToken)
    {
        var subjects = await _handler.HandleAsync(cancellationToken);
        return Ok(subjects);
    }
}
