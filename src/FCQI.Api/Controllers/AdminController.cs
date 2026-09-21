using FCQI.Application.Administration;
using FCQI.Application.Advisors.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace FCQI.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly GetAdminAdvisorsQueryHandler _list;
    private readonly ReplaceAdvisorSubjectsCommandHandler _replace;

    public AdminController(GetAdminAdvisorsQueryHandler list, ReplaceAdvisorSubjectsCommandHandler replace)
    {
        _list = list;
        _replace = replace;
    }

    [HttpGet("advisors")]
    public async Task<ActionResult<IEnumerable<AdvisorDto>>> GetAdvisors(CancellationToken cancellationToken)
        => Ok(await _list.HandleAsync(cancellationToken));

    [HttpPut("advisors/{id:int}/subjects")]
    public async Task<IActionResult> PutSubjects(int id, [FromBody] IReadOnlyList<int> subjectIds, CancellationToken cancellationToken)
    {
        try
        {
            await _replace.HandleAsync(id, subjectIds, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
