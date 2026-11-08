using FCQI.Application.Advisors.Dtos;
using FCQI.Application.Advisors.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCQI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class LocationsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LocationDto>>> Get(
        [FromServices] GetLocationsQueryHandler locations, CancellationToken cancellationToken)
        => Ok(await locations.HandleAsync(cancellationToken));
}
