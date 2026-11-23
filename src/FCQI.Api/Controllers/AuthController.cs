using FCQI.Application.Auth;
using FCQI.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCQI.Api.Controllers;

// ID token que entrega Google Identity Services al cliente.
public record GoogleLoginRequest(string IdToken);

// Correo y contraseña del login provisional (la contraseña aún no se verifica).
public record LoginRequest(string Email, string Password);

// Correo del perfil elegido en el login de demostración.
public record DemoLoginRequest(string Email);

// Inicio de sesión: valida la identidad (Google o modo sin contraseña),
// resuelve los roles y emite el JWT propio de la API.
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

    // Recibe el validador de Google, el resolvedor de perfiles, la fábrica de JWT y la configuración.
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

    // Configuración pública del login, para que el cliente decida qué
    // botones mostrar sin incrustar valores en su compilación.
    [HttpGet("config")]
    public ActionResult GetConfig()
        => Ok(new
        {
            googleClientId = _configuration["Authentication:Google:ClientId"] ?? "",
            googleConfigured = !string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientId"]),
            passwordlessLogin = DemoLoginEnabled
        });

    // Acceso sin contraseña: /login no verifica la contraseña (aún no hay
    // directorio UABC) y /demo ni la pide, así que cualquiera que conozca un
    // correo entraría como esa persona. Solo se permite mientras no haya
    // Google configurado. Activo por omisión (fase de pruebas); se apaga con
    // Authentication:PasswordlessLogin=false (FCQI_PASSWORDLESS_LOGIN en .env).
    private bool DemoLoginEnabled
        => string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientId"])
           && _configuration.GetValue("Authentication:PasswordlessLogin", true);

    // 403 uniforme para los accesos sin contraseña cuando están apagados.
    private ObjectResult PasswordlessDisabled()
        => StatusCode(StatusCodes.Status403Forbidden, new
        {
            message = "El acceso sin contraseña está desactivado en este servidor. Entra con tu cuenta de Google @uabc.edu.mx."
        });

    // Perfiles para el selector de demostración; 404 cuando el modo está apagado para no revelar la lista.
    [HttpGet("demo-profiles")]
    public async Task<ActionResult<IEnumerable<DemoProfileDto>>> DemoProfiles(CancellationToken cancellationToken)
    {
        if (!DemoLoginEnabled)
        {
            return NotFound();
        }

        return Ok(await _profiles.ListDemoProfilesAsync(cancellationToken));
    }

    // Login con correo y contraseña; exige que la contraseña no esté vacía aunque todavía no se verifique.
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

    // Login de demostración: entra como el perfil elegido, sin contraseña.
    [HttpPost("demo")]
    public async Task<ActionResult<AuthResult>> Demo([FromBody] DemoLoginRequest request, CancellationToken cancellationToken)
    {
        if (!DemoLoginEnabled)
        {
            return PasswordlessDisabled();
        }

        return await SignInAsync(request.Email, cancellationToken);
    }

    // Resuelve los roles y emite el token. Se resuelve con un token
    // provisional y luego se sustituye con with, porque el JWT necesita
    // los roles ya resueltos.
    private async Task<ActionResult<AuthResult>> SignInAsync(string email, CancellationToken cancellationToken)
    {
        var resolved = await _profiles.ResolveAsync(email, "pending", cancellationToken);
        if (resolved is null)
        {
            return Unauthorized(new { message = "El correo UABC no está dado de alta en el programa de asesorías." });
        }

        return Ok(resolved with { Token = _jwt.Create(resolved) });
    }

    // Login con Google: el correo sale del token validado por Google, nunca
    // del cuerpo de la petición, así que no puede falsificarse.
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
