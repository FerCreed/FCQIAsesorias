using FCQI.Application.Auth;
using FCQI.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCQI.Api.Controllers;

public record GoogleLoginRequest(string IdToken);
public record DemoLoginRequest(string Email);

[ApiController]
[Route("api/auth")]
// Único controlador público: aquí es donde se obtiene el token.
[AllowAnonymous]
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

    /// <summary>
    /// El selector de perfiles expone el directorio de personas del programa,
    /// así que solo existe mientras no haya OAuth configurado. En cuanto se
    /// define un ClientId de Google, estos dos endpoints desaparecen.
    /// </summary>
    private bool DemoLoginEnabled
        => string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientId"]);

    [HttpGet("demo-profiles")]
    public async Task<ActionResult<IEnumerable<DemoProfileDto>>> DemoProfiles(CancellationToken cancellationToken)
    {
        if (!DemoLoginEnabled)
        {
            return NotFound();
        }

        return Ok(await _profiles.ListDemoProfilesAsync(cancellationToken));
    }

    [HttpPost("demo")]
    public async Task<ActionResult<AuthResult>> Demo([FromBody] DemoLoginRequest request, CancellationToken cancellationToken)
    {
        if (!DemoLoginEnabled)
        {
            return NotFound();
        }

        try
        {
            var resolved = await _profiles.ResolveAsync(request.Email, "pending", cancellationToken);
            if (resolved is null)
            {
                return Unauthorized(new { message = "El correo UABC no está dado de alta en el programa de asesorías." });
            }

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
            if (resolved is null)
            {
                return Unauthorized(new
                {
                    message = "El correo @uabc.edu.mx es válido, pero no está registrado como alumno, asesor o directivo del programa."
                });
            }

            var token = _jwt.Create(resolved);
            return Ok(resolved with { Token = token });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
