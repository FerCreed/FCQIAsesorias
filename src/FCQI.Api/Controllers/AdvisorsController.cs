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

// Catálogo de asesores, horarios propios del asesor, contacto y su resumen.
// Requiere autenticación salvo en las consultas públicas del catálogo.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdvisorsController : ControllerBase
{
    private readonly CurrentUser _user;

    // Recibe la identidad de quien llama, leída del token.
    public AdvisorsController(CurrentUser user) => _user = user;

    // Catálogo público: asesores asignados a una materia (BPMN 4).
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<AdvisorDto>>> Get(
        [FromServices] GetAdvisorsQueryHandler advisors, [FromQuery] int? subjectId, CancellationToken cancellationToken)
        => Ok(await advisors.HandleAsync(subjectId, cancellationToken));

    // Catálogo público: horarios activos de un asesor (BPMN 3).
    [HttpGet("{id:int}/availabilities")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<AvailabilityDto>>> GetAvailabilities(
        [FromServices] GetAvailabilitiesQueryHandler availabilities, int id, CancellationToken cancellationToken)
        => Ok(await availabilities.HandleAsync(id, cancellationToken));

    // Mensaje del alumno al asesor por correo (BPMN 15). Máximo 5 por minuto por persona.
    [HttpPost("{id:int}/messages")]
    [EnableRateLimiting(ApiSetup.ContactPolicy)]
    public async Task<IActionResult> Contact(
        [FromServices] ContactAdvisorCommandHandler contact,
        int id, [FromBody] ContactAdvisorRequest request, CancellationToken cancellationToken)
    {
        await contact.HandleAsync(id, request.Message, _user.ToCaller(), cancellationToken);
        return NoContent();
    }

    // Los bloques del asesor que llama.
    [HttpGet("me/availabilities")]
    [Authorize(Roles = UserRoles.Advisor)]
    public async Task<ActionResult<IEnumerable<AvailabilityDto>>> GetMine(
        [FromServices] GetAvailabilitiesQueryHandler availabilities, CancellationToken cancellationToken)
        => Ok(await availabilities.HandleAsync(_user.PersonId, cancellationToken));

    // Configurar disponibilidad (BPMN 7).
    [HttpPost("me/availabilities")]
    [Authorize(Roles = UserRoles.Advisor)]
    public async Task<IActionResult> Create(
        [FromServices] AvailabilityCommandHandler blocks,
        [FromBody] SaveAvailabilityRequest request, CancellationToken cancellationToken)
    {
        var id = await blocks.CreateAsync(request, _user.ToCaller(), cancellationToken);
        // 201 con la ubicación de la lista de bloques, como pide REST para una alta.
        return CreatedAtAction(nameof(GetMine), new { id }, new { id });
    }

    // Editar horario (BPMN 13).
    [HttpPut("me/availabilities/{id:int}")]
    [Authorize(Roles = UserRoles.Advisor)]
    public async Task<IActionResult> Update(
        [FromServices] AvailabilityCommandHandler blocks,
        int id, [FromBody] SaveAvailabilityRequest request, CancellationToken cancellationToken)
    {
        await blocks.UpdateAsync(id, request, _user.ToCaller(), cancellationToken);
        return NoContent();
    }

    // Retirar un bloque (BPMN 16). Baja lógica.
    [HttpDelete("me/availabilities/{id:int}")]
    [Authorize(Roles = UserRoles.Advisor)]
    public async Task<IActionResult> Retire(
        [FromServices] AvailabilityCommandHandler blocks, int id, CancellationToken cancellationToken)
    {
        await blocks.RetireAsync(id, _user.ToCaller(), cancellationToken);
        return NoContent();
    }

    // Resumen del asesor que llama: sus asesorías del periodo y sus horas trabajadas.
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
