using FCQI.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCQI.Api.Controllers;

// Información pública del ciclo escolar.
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class TermsController : ControllerBase
{
    // Ciclo vigente: el cliente limita los calendarios a sus fechas.
    [HttpGet("current")]
    public async Task<ActionResult<AcademicTermInfo>> Current([FromServices] CurrentTerm term, CancellationToken cancellationToken)
        => Ok(await term.GetAsync(cancellationToken));
}
