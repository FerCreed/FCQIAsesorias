using FCQI.Api.Security;
using FCQI.Application.Administration;
using FCQI.Application.Interfaces;
using FCQI.Application.Reports;
using FCQI.Application.Advisors.Dtos;
using FCQI.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCQI.Api.Controllers;

[ApiController]
[Route("api/admin")]
// Solo dirección: asignación de asesores y reportes del programa.
[Authorize(Roles = UserRoles.Admin)]
public class AdminController : ControllerBase
{
    private readonly CurrentUser _user;

    public AdminController(CurrentUser user) => _user = user;

    [HttpGet("advisors")]
    public async Task<ActionResult<IEnumerable<AdvisorDto>>> GetAdvisors(
        [FromServices] GetAdminAdvisorsQueryHandler list, CancellationToken cancellationToken)
        => Ok(await list.HandleAsync(cancellationToken));

    /// <summary>Asignar asesores a materias (BPMN 2).</summary>
    [HttpPut("advisors/{id:int}/subjects")]
    public async Task<IActionResult> PutSubjects(
        [FromServices] ReplaceAdvisorSubjectsCommandHandler replace,
        int id, [FromBody] IReadOnlyList<int> subjectIds, CancellationToken cancellationToken)
    {
        await replace.HandleAsync(id, subjectIds, cancellationToken);
        return NoContent();
    }

    /// <summary>Tablero (BPMN 6): métricas de las asesorías creadas en el periodo.</summary>
    [HttpGet("reports")]
    public async Task<ActionResult<object>> Report(
        [FromServices] SessionsExportQueryHandler export, [FromQuery] ReportQuery query, CancellationToken cancellationToken)
    {
        var result = await export.HandleAsync(query.ToFilter(), _user.ToCaller(), cancellationToken);
        return Ok(new { result.Period, result.Metrics });
    }

    /// <summary>Todas las asesorías creadas desde <c>from</c>, con todos sus datos (CSV o PDF con métricas).</summary>
    [HttpGet("sessions.{format:regex(^(pdf|csv)$)}")]
    public async Task<IActionResult> Sessions(
        [FromServices] SessionsExportQueryHandler export, [FromServices] IReportRenderer pdf,
        string format, [FromQuery] ReportQuery query, CancellationToken cancellationToken)
    {
        var result = await export.HandleAsync(query.ToFilter(), _user.ToCaller(), cancellationToken);
        var name = $"asesorias-{result.Period.From:yyyyMMdd}-{result.Period.To:yyyyMMdd}";
        return format == "pdf" ? this.Pdf(pdf.Sessions(result), name) : this.Csv(ReportCsv.Write(result.Sessions), name);
    }

    /// <summary>Resumen de cualquier asesor: asesorías del periodo y horas trabajadas.</summary>
    [HttpGet("advisors/{id:int}/report.{format:regex(^(pdf|csv)$)}")]
    public async Task<IActionResult> AdvisorReport(
        [FromServices] AdvisorSummaryQueryHandler summaries, [FromServices] IReportRenderer pdf,
        int id, string format, [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken cancellationToken)
    {
        var summary = await summaries.HandleAsync(id, from, to, _user.ToCaller(), cancellationToken);
        var name = $"resumen-asesor-{id}-{from:yyyyMMdd}-{to:yyyyMMdd}";
        return format == "pdf" ? this.Pdf(pdf.AdvisorSummary(summary), name) : this.Csv(ReportCsv.Write(summary.Sessions), name);
    }
}

/// <summary>Filtros de los reportes de dirección. <c>From</c> es obligatoria.</summary>
public record ReportQuery(DateOnly From, DateOnly? To, int? AdvisorId, int? SubjectId, string? Status)
{
    public ReportFilter ToFilter() => new(From, To, AdvisorId, SubjectId, Status);
}
