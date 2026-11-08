using System.Text;
using FCQI.Api.Security;
using FCQI.Application.Sessions.Commands;
using FCQI.Application.Sessions.Dtos;
using FCQI.Application.Sessions.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCQI.Api.Controllers;

/// <summary>
/// Asesorías. Los handlers se inyectan por acción ([FromServices]) para que el
/// controlador no cargue con todos en cada petición. Los errores de negocio
/// los traduce <see cref="Filters.ApiExceptionFilter"/>.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SessionsController : ControllerBase
{
    private readonly CurrentUser _user;

    public SessionsController(CurrentUser user) => _user = user;

    /// <summary>
    /// Un alumno ve sus solicitudes, un asesor las que le tocan y dirección
    /// todas. <c>scope</c>: <c>upcoming</c> o <c>history</c>.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SessionDto>>> Get(
        [FromServices] GetSessionsQueryHandler query,
        [FromQuery] int? studentId, [FromQuery] int? advisorId,
        [FromQuery] string? status, [FromQuery] string? scope,
        CancellationToken cancellationToken)
        => Ok(await query.HandleAsync(studentId, advisorId, status, _user.ToCaller(), scope, cancellationToken));

    /// <summary>El alumno solicita (BPMN 8). La identidad sale del token.</summary>
    [HttpPost]
    public async Task<ActionResult<SessionDto>> Post(
        [FromServices] CreateSessionCommandHandler create,
        [FromBody] CreateSessionRequest request, CancellationToken cancellationToken)
    {
        var created = await create.HandleAsync(request, _user.ToCaller(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { studentId = created.StudentId }, created);
    }

    /// <summary>Confirmar, rechazar o cancelar (BPMN 11, 12, 18).</summary>
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> PatchStatus(
        [FromServices] UpdateSessionStatusCommandHandler update,
        int id, [FromBody] UpdateSessionStatusRequest request, CancellationToken cancellationToken)
    {
        await update.HandleAsync(id, request.Status, _user.ToCaller(), cancellationToken);
        return NoContent();
    }

    /// <summary>Reprogramar (BPMN 11). Devuelve la cita nueva.</summary>
    [HttpPatch("{id:int}/schedule")]
    public async Task<ActionResult<SessionDto>> Reschedule(
        [FromServices] RescheduleSessionCommandHandler reschedule,
        int id, [FromBody] RescheduleSessionRequest request, CancellationToken cancellationToken)
        => Ok(await reschedule.HandleAsync(id, request, _user.ToCaller(), cancellationToken));

    /// <summary>Bitácora y asistencia (BPMN 9). La asesoría queda Realizada.</summary>
    [HttpPost("{id:int}/log")]
    public async Task<IActionResult> Log(
        [FromServices] RecordSessionLogCommandHandler log,
        int id, [FromBody] SessionLogRequest request, CancellationToken cancellationToken)
    {
        await log.HandleAsync(id, request, _user.ToCaller(), cancellationToken);
        return NoContent();
    }

    /// <summary>La cita en formato iCalendar para agregarla a cualquier calendario (BPMN 12).</summary>
    [HttpGet("{id:int}/calendar.ics")]
    public async Task<IActionResult> Calendar(
        [FromServices] SessionCalendarQueryHandler calendar, int id, CancellationToken cancellationToken)
        => File(Encoding.UTF8.GetBytes(await calendar.HandleAsync(id, _user.ToCaller(), cancellationToken)),
            "text/calendar", $"asesoria-{id}.ics");
}
