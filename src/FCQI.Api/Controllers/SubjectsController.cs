using FCQI.Application.Subjects.Dtos;
using FCQI.Application.Subjects.Queries;
using Microsoft.AspNetCore.Mvc;

namespace FCQI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubjectsController : ControllerBase
{
    private readonly GetSubjectsQueryHandler _handler;

    public SubjectsController(GetSubjectsQueryHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SubjectDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SubjectDto>>> Get(CancellationToken cancellationToken)
    {
        var subjects = await _handler.HandleAsync(cancellationToken);
        return Ok(subjects);
    }
}
