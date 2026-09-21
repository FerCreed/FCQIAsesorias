using FCQI.Application.Advisors.Dtos;
using FCQI.Application.Advisors.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCQI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// Catálogo del programa: cualquier usuario autenticado puede consultarlo.
[Authorize]
public class AdvisorsController : ControllerBase
{
    private readonly GetAdvisorsQueryHandler _advisors;
    private readonly GetAvailabilitiesQueryHandler _availabilities;

    public AdvisorsController(GetAdvisorsQueryHandler advisors, GetAvailabilitiesQueryHandler availabilities)
    {
        _advisors = advisors;
        _availabilities = availabilities;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdvisorDto>>> Get([FromQuery] int? subjectId, CancellationToken cancellationToken)
        => Ok(await _advisors.HandleAsync(subjectId, cancellationToken));

    [HttpGet("{id:int}/availabilities")]
    public async Task<ActionResult<IEnumerable<AvailabilityDto>>> GetAvailabilities(int id, CancellationToken cancellationToken)
        => Ok(await _availabilities.HandleAsync(id, cancellationToken));
}
