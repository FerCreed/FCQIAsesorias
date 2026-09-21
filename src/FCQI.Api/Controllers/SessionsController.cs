using FCQI.Application.Sessions.Commands;
using FCQI.Application.Sessions.Dtos;
using FCQI.Application.Sessions.Queries;
using Microsoft.AspNetCore.Mvc;

namespace FCQI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionsController : ControllerBase
{
    private readonly GetSessionsQueryHandler _query;
    private readonly CreateSessionCommandHandler _create;
    private readonly UpdateSessionStatusCommandHandler _update;

    public SessionsController(
        GetSessionsQueryHandler query,
        CreateSessionCommandHandler create,
        UpdateSessionStatusCommandHandler update)
    {
        _query = query;
        _create = create;
        _update = update;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SessionDto>>> Get(
        [FromQuery] int? studentId,
        [FromQuery] int? advisorId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
        => Ok(await _query.HandleAsync(studentId, advisorId, status, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<SessionDto>> Post([FromBody] CreateSessionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _create.HandleAsync(request, cancellationToken);
            return CreatedAtAction(nameof(Get), new { studentId = created.StudentId }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> PatchStatus(int id, [FromBody] UpdateSessionStatusRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _update.HandleAsync(id, request.Status, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
