using FCQI.Api.Security;
using FCQI.Application.Advisors.Commands;
using FCQI.Application.Advisors.Dtos;
using FCQI.Application.Advisors.Queries;
using FCQI.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCQI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdvisorsController : ControllerBase
{
    private readonly CurrentUser _user;

    public AdvisorsController(CurrentUser user) => _user = user;

    /// <summary>Catálogo público: tutores asignados a una materia (BPMN 4).</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<AdvisorDto>>> Get(
        [FromServices] GetAdvisorsQueryHandler advisors, [FromQuery] int? subjectId, CancellationToken cancellationToken)
        => Ok(await advisors.HandleAsync(subjectId, cancellationToken));

    /// <summary>Catálogo público: horarios activos de un tutor (BPMN 3).</summary>
    [HttpGet("{id:int}/availabilities")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<AvailabilityDto>>> GetAvailabilities(
        [FromServices] GetAvailabilitiesQueryHandler availabilities, int id, CancellationToken cancellationToken)
        => Ok(await availabilities.HandleAsync(id, cancellationToken));

    /// <summary>Mensaje del alumno al tutor por correo (BPMN 15).</summary>
    [HttpPost("{id:int}/messages")]
    public async Task<IActionResult> Contact(
        [FromServices] ContactAdvisorCommandHandler contact,
        int id, [FromBody] ContactAdvisorRequest request, CancellationToken cancellationToken)
    {
        await contact.HandleAsync(id, request.Message, _user.ToCaller(), cancellationToken);
        return NoContent();
    }

    /// <summary>Los bloques del asesor que llama.</summary>
    [HttpGet("me/availabilities")]
    [Authorize(Roles = UserRoles.Advisor)]
    public async Task<ActionResult<IEnumerable<AvailabilityDto>>> GetMine(
        [FromServices] GetAvailabilitiesQueryHandler availabilities, CancellationToken cancellationToken)
        => Ok(await availabilities.HandleAsync(_user.PersonId, cancellationToken));

    /// <summary>Configurar disponibilidad (BPMN 7).</summary>
    [HttpPost("me/availabilities")]
    [Authorize(Roles = UserRoles.Advisor)]
    public async Task<IActionResult> Create(
        [FromServices] AvailabilityCommandHandler blocks,
        [FromBody] SaveAvailabilityRequest request, CancellationToken cancellationToken)
    {
        var id = await blocks.CreateAsync(request, _user.ToCaller(), cancellationToken);
        return CreatedAtAction(nameof(GetMine), new { id }, new { id });
    }

    /// <summary>Editar horario (BPMN 13).</summary>
    [HttpPut("me/availabilities/{id:int}")]
    [Authorize(Roles = UserRoles.Advisor)]
    public async Task<IActionResult> Update(
        [FromServices] AvailabilityCommandHandler blocks,
        int id, [FromBody] SaveAvailabilityRequest request, CancellationToken cancellationToken)
    {
        await blocks.UpdateAsync(id, request, _user.ToCaller(), cancellationToken);
        return NoContent();
    }

    /// <summary>Retirar un bloque (BPMN 16). Baja lógica.</summary>
    [HttpDelete("me/availabilities/{id:int}")]
    [Authorize(Roles = UserRoles.Advisor)]
    public async Task<IActionResult> Retire(
        [FromServices] AvailabilityCommandHandler blocks, int id, CancellationToken cancellationToken)
    {
        await blocks.RetireAsync(id, _user.ToCaller(), cancellationToken);
        return NoContent();
    }
}
