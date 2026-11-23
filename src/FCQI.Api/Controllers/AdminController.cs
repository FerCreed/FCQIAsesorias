using FCQI.Api.Security;
using FCQI.Application.Administration;
using FCQI.Application.Interfaces;
using FCQI.Application.Reports;
using FCQI.Application.Advisors.Dtos;
using FCQI.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCQI.Api.Controllers;

// Endpoints de dirección. Los handlers se inyectan por acción con
// [FromServices] para no construirlos todos en cada petición.
[ApiController]
[Route("api/admin")]
// Solo dirección: asignación de asesores y reportes del programa.
[Authorize(Roles = UserRoles.Admin)]
public class AdminController : ControllerBase
{
    private readonly CurrentUser _user;

    // Recibe la identidad de quien llama, leída del token.
    public AdminController(CurrentUser user) => _user = user;

    // Todos los asesores, activos o no, con sus materias del ciclo vigente.
    [HttpGet("advisors")]
    public async Task<ActionResult<IEnumerable<AdvisorDto>>> GetAdvisors(
        [FromServices] GetAdminAdvisorsQueryHandler list, CancellationToken cancellationToken)
        => Ok(await list.HandleAsync(cancellationToken));

    // Asignar asesores a materias (BPMN 2).
    [HttpPut("advisors/{id:int}/subjects")]
    public async Task<IActionResult> PutSubjects(
        [FromServices] ReplaceAdvisorSubjectsCommandHandler replace,
        int id, [FromBody] IReadOnlyList<int> subjectIds, CancellationToken cancellationToken)
    {
        await replace.HandleAsync(id, subjectIds, cancellationToken);
        return NoContent();
    }

    // Tablero (BPMN 6): métricas de las asesorías creadas en el periodo.
    [HttpGet("reports")]
    public async Task<ActionResult<object>> Report(
        [FromServices] SessionsExportQueryHandler export, [FromQuery] ReportQuery query, CancellationToken cancellationToken)
    {
        var result = await export.HandleAsync(query.ToFilter(), _user.ToCaller(), cancellationToken);
        // Solo periodo y métricas: el tablero no necesita las filas, que pueden ser miles.
        return Ok(new { result.Period, result.Metrics });
    }

    // Todas las asesorías creadas desde from, con todos sus datos (CSV o PDF con métricas).
    [HttpGet("sessions.{format:regex(^(pdf|csv)$)}")]
    public async Task<IActionResult> Sessions(
        [FromServices] SessionsExportQueryHandler export, [FromServices] IReportRenderer pdf,
        string format, [FromQuery] ReportQuery query, CancellationToken cancellationToken)
    {
        var result = await export.HandleAsync(query.ToFilter(), _user.ToCaller(), cancellationToken);
        var name = $"asesorias-{result.Period.From:yyyyMMdd}-{result.Period.To:yyyyMMdd}";
        return format == "pdf" ? this.Pdf(pdf.Sessions(result), name) : this.Csv(ReportCsv.Write(result.Sessions), name);
    }

    // Resumen de cualquier asesor: asesorías del periodo y horas trabajadas.
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

// Filtros de los reportes de dirección. From es obligatoria.
public record ReportQuery(DateOnly From, DateOnly? To, int? AdvisorId, int? SubjectId, string? Status)
{
    // Adapta los parámetros HTTP al filtro de la capa de aplicación, que no conoce la API.
    public ReportFilter ToFilter() => new(From, To, AdvisorId, SubjectId, Status);
}
