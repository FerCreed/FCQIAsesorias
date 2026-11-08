using System.Text;
using FCQI.Application.Administration;
using FCQI.Application.Advisors.Dtos;
using FCQI.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCQI.Api.Controllers;

[ApiController]
[Route("api/admin")]
// Solo dirección: asignación de tutores y reportes del programa.
[Authorize(Roles = UserRoles.Admin)]
public class AdminController : ControllerBase
{
    [HttpGet("advisors")]
    public async Task<ActionResult<IEnumerable<AdvisorDto>>> GetAdvisors(
        [FromServices] GetAdminAdvisorsQueryHandler list, CancellationToken cancellationToken)
        => Ok(await list.HandleAsync(cancellationToken));

    /// <summary>Asignar tutores a materias (BPMN 2).</summary>
    [HttpPut("advisors/{id:int}/subjects")]
    public async Task<IActionResult> PutSubjects(
        [FromServices] ReplaceAdvisorSubjectsCommandHandler replace,
        int id, [FromBody] IReadOnlyList<int> subjectIds, CancellationToken cancellationToken)
    {
        await replace.HandleAsync(id, subjectIds, cancellationToken);
        return NoContent();
    }

    /// <summary>Métricas del ciclo (BPMN 6).</summary>
    [HttpGet("reports")]
    public async Task<ActionResult<AdvisoryReportDto>> Report(
        [FromServices] AdvisoryReportQueryHandler reports,
        [FromQuery] int? advisorId, [FromQuery] int? subjectId, CancellationToken cancellationToken)
        => Ok(await reports.HandleAsync(advisorId, subjectId, cancellationToken));

    /// <summary>Constancia de horas por asesor en CSV (BPMN 6).</summary>
    [HttpGet("reports.csv")]
    public async Task<IActionResult> ReportCsv(
        [FromServices] AdvisoryReportQueryHandler reports,
        [FromQuery] int? advisorId, [FromQuery] int? subjectId, CancellationToken cancellationToken)
    {
        var report = await reports.HandleAsync(advisorId, subjectId, cancellationToken);
        // BOM para que Excel abra los acentos correctamente.
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(AdvisoryReportQueryHandler.ToCsv(report))).ToArray();
        return File(bytes, "text/csv", $"constancia-horas-{report.Term}.csv");
    }
}
