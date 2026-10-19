using FCQI.Application.Auth;
using FCQI.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace FCQI.Api.Controllers;

public record GoogleLoginRequest(string IdToken);
public record LoginRequest(string Email, string Password);

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IGoogleTokenValidator _google;
    private readonly ResolveProfileHandler _profiles;
    private readonly JwtTokenFactory _jwt;
    private readonly IConfiguration _configuration;

    public AuthController(
        IGoogleTokenValidator google,
        ResolveProfileHandler profiles,
        JwtTokenFactory jwt,
        IConfiguration configuration)
    {
        _google = google;
        _profiles = profiles;
        _jwt = jwt;
        _configuration = configuration;
    }

    [HttpGet("config")]
    public ActionResult GetConfig()
        => Ok(new
        {
            googleClientId = _configuration["Authentication:Google:ClientId"] ?? "",
            googleConfigured = !string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientId"])
        });

    [HttpGet("demo-profiles")]
    public async Task<ActionResult<IEnumerable<DemoProfileDto>>> DemoProfiles(CancellationToken cancellationToken)
        => Ok(await _profiles.ListDemoProfilesAsync(cancellationToken));

    [HttpPost("login")]
    [HttpPost("demo")]
    public async Task<ActionResult<AuthResult>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Ingresa una contraseña." });
            }

            var resolved = await _profiles.ResolveAsync(request.Email, "pending", cancellationToken);
            var token = _jwt.Create(resolved);
            return Ok(resolved with { Token = token });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("google")]
    public async Task<ActionResult<AuthResult>> Google([FromBody] GoogleLoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var googleUser = await _google.ValidateAsync(request.IdToken, cancellationToken);
            var resolved = await _profiles.ResolveAsync(googleUser.Email, "pending", cancellationToken);
            var token = _jwt.Create(resolved);
            return Ok(resolved with { Token = token });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
