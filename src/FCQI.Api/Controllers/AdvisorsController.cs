using FCQI.Api.Configuration;
using FCQI.Api.Security;
using FCQI.Application.Advisors.Commands;
using FCQI.Application.Advisors.Dtos;
using FCQI.Application.Advisors.Queries;
using FCQI.Application.Interfaces;
using FCQI.Application.Reports;
using FCQI.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FCQI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdvisorsController : ControllerBase
{
    private readonly CurrentUser _user;

    public AdvisorsController(CurrentUser user) => _user = user;

    /// <summary>Catálogo público: asesores asignados a una materia (BPMN 4).</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<AdvisorDto>>> Get(
        [FromServices] GetAdvisorsQueryHandler advisors, [FromQuery] int? subjectId, CancellationToken cancellationToken)
        => Ok(await advisors.HandleAsync(subjectId, cancellationToken));

    /// <summary>Catálogo público: horarios activos de un asesor (BPMN 3).</summary>
    [HttpGet("{id:int}/availabilities")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<AvailabilityDto>>> GetAvailabilities(
        [FromServices] GetAvailabilitiesQueryHandler availabilities, int id, CancellationToken cancellationToken)
        => Ok(await availabilities.HandleAsync(id, cancellationToken));

    /// <summary>Mensaje del alumno al asesor por correo (BPMN 15). Máximo 5 por minuto por persona.</summary>
    [HttpPost("{id:int}/messages")]
    [EnableRateLimiting(ApiSetup.ContactPolicy)]
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

    /// <summary>Resumen del asesor que llama: sus asesorías del periodo y sus horas trabajadas.</summary>
    [HttpGet("me/report.{format:regex(^(pdf|csv)$)}")]
    [Authorize(Roles = UserRoles.Advisor)]
    public async Task<IActionResult> MyReport(
        [FromServices] AdvisorSummaryQueryHandler summaries, [FromServices] IReportRenderer pdf,
        string format, [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken cancellationToken)
    {
        var summary = await summaries.HandleAsync(_user.PersonId, from, to, _user.ToCaller(), cancellationToken);
        var name = $"resumen-asesorias-{from:yyyyMMdd}-{to:yyyyMMdd}";
        return format == "pdf" ? this.Pdf(pdf.AdvisorSummary(summary), name) : this.Csv(ReportCsv.Write(summary.Sessions), name);
    }
}
