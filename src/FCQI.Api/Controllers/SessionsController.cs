using FCQI.Api.Security;
using FCQI.Application.Common;
using FCQI.Application.Sessions.Commands;
using FCQI.Application.Sessions.Dtos;
using FCQI.Application.Sessions.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCQI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SessionsController : ControllerBase
{
    private readonly GetSessionsQueryHandler _query;
    private readonly CreateSessionCommandHandler _create;
    private readonly UpdateSessionStatusCommandHandler _update;
    private readonly CurrentUser _user;

    public SessionsController(
        GetSessionsQueryHandler query,
        CreateSessionCommandHandler create,
        UpdateSessionStatusCommandHandler update,
        CurrentUser user)
    {
        _query = query;
        _create = create;
        _update = update;
        _user = user;
    }

    private Caller Caller => new(_user.PersonId, _user.IsAdmin, _user.IsAdvisor, _user.IsStudent);

    /// <summary>
    /// Un alumno ve sus solicitudes, un asesor las que le tocan y dirección
    /// todas. Pedir las de otra persona devuelve 403.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SessionDto>>> Get(
        [FromQuery] int? studentId,
        [FromQuery] int? advisorId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _query.HandleAsync(studentId, advisorId, status, Caller, cancellationToken));
        }
        catch (ForbiddenOperationException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// El alumno solicita. StudentId del cuerpo se ignora salvo para
    /// dirección: para todos los demás la identidad sale del token.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SessionDto>> Post([FromBody] CreateSessionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _create.HandleAsync(request, Caller, cancellationToken);
            return CreatedAtAction(nameof(Get), new { studentId = created.StudentId }, created);
        }
        catch (ForbiddenOperationException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// El asesor dueño confirma o rechaza; el alumno dueño solo cancela;
    /// dirección puede todo.
    /// </summary>
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> PatchStatus(int id, [FromBody] UpdateSessionStatusRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _update.HandleAsync(id, request.Status, Caller, cancellationToken);
            return NoContent();
        }
        catch (ForbiddenOperationException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
