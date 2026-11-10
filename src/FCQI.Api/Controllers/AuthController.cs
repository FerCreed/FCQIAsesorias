using FCQI.Application.Auth;
using FCQI.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCQI.Api.Controllers;

public record GoogleLoginRequest(string IdToken);
public record LoginRequest(string Email, string Password);
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
    private readonly IHostEnvironment _environment;

    public AuthController(
        IGoogleTokenValidator google,
        ResolveProfileHandler profiles,
        JwtTokenFactory jwt,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _google = google;
        _profiles = profiles;
        _jwt = jwt;
        _configuration = configuration;
        _environment = environment;
    }

    [HttpGet("config")]
    public ActionResult GetConfig()
        => Ok(new
        {
            googleClientId = _configuration["Authentication:Google:ClientId"] ?? "",
            googleConfigured = !string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientId"]),
            passwordlessLogin = DemoLoginEnabled
        });

    /// <summary>
    /// Acceso sin contraseña: /login no verifica la contraseña (aún no hay
    /// directorio UABC) y /demo ni la pide, así que cualquiera que conozca un
    /// correo entraría como esa persona. Solo se permite mientras no haya
    /// Google configurado y, además, en desarrollo o si se activa a propósito
    /// con Authentication:PasswordlessLogin=true (FCQI_PASSWORDLESS_LOGIN en
    /// .env). En producción expuesta a internet debe quedar apagado.
    /// </summary>
    private bool DemoLoginEnabled
        => string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientId"])
           && _configuration.GetValue("Authentication:PasswordlessLogin", _environment.IsDevelopment());

    private ObjectResult PasswordlessDisabled()
        => StatusCode(StatusCodes.Status403Forbidden, new
        {
            message = "El acceso sin contraseña está desactivado en este servidor. Entra con tu cuenta de Google @uabc.edu.mx."
        });

    [HttpGet("demo-profiles")]
    public async Task<ActionResult<IEnumerable<DemoProfileDto>>> DemoProfiles(CancellationToken cancellationToken)
    {
        if (!DemoLoginEnabled)
        {
            return NotFound();
        }

        return Ok(await _profiles.ListDemoProfilesAsync(cancellationToken));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResult>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (!DemoLoginEnabled)
        {
            return PasswordlessDisabled();
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Ingresa una contraseña." });
        }

        return await SignInAsync(request.Email, cancellationToken);
    }

    [HttpPost("demo")]
    public async Task<ActionResult<AuthResult>> Demo([FromBody] DemoLoginRequest request, CancellationToken cancellationToken)
    {
        if (!DemoLoginEnabled)
        {
            return PasswordlessDisabled();
        }

        return await SignInAsync(request.Email, cancellationToken);
    }

    private async Task<ActionResult<AuthResult>> SignInAsync(string email, CancellationToken cancellationToken)
    {
        var resolved = await _profiles.ResolveAsync(email, "pending", cancellationToken);
        if (resolved is null)
        {
            return Unauthorized(new { message = "El correo UABC no está dado de alta en el programa de asesorías." });
        }

        return Ok(resolved with { Token = _jwt.Create(resolved) });
    }

    [HttpPost("google")]
    public async Task<ActionResult<AuthResult>> Google([FromBody] GoogleLoginRequest request, CancellationToken cancellationToken)
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

        return Ok(resolved with { Token = _jwt.Create(resolved) });
    }
}
