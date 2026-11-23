using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using FCQI.Api.Filters;
using FCQI.Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace FCQI.Api.Configuration;

// Configuración web de la API: controladores, JWT y CORS.
public static class ApiSetup
{
    // Política del limitador para los mensajes al asesor (BPMN 15).
    public const string ContactPolicy = "contacto";

    // Mensajes por persona y minuto. Evita usar el sistema para saturar el correo de un profesor.
    public const int ContactPermitsPerMinute = 5;

    // Registra todo lo propio de la capa web. Es método de extensión para que
    // Program.cs quede como una cadena legible de capas (API, aplicación,
    // infraestructura).
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        // El filtro global traduce las excepciones de negocio a 400/403/404 en todos los controladores.
        services.AddControllers(options => options.Filters.Add<ApiExceptionFilter>());
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        // Identidad de quien llama, leída del JWT.
        services.AddHttpContextAccessor();
        services.AddScoped<CurrentUser>();

        // Se valida emisor, audiencia y firma para rechazar tokens de otros sistemas o alterados.
        // Los valores por omisión son solo de desarrollo; en producción vienen de la configuración.
        var jwt = configuration.GetSection("Authentication:Jwt");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwt["Issuer"] ?? "FCQI.Api",
                ValidAudience = jwt["Audience"] ?? "FCQI.Web",
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwt["Key"] ?? "FCQI-dev-jwt-key-change-me-32chars!"))
            });
        services.AddAuthorization();

        // En Docker el frontend se sirve desde el mismo origen que /api; CORS
        // solo hace falta para el cliente de escritorio en desarrollo
        // (localhost) y para los orígenes extra de Cors:Origins.
        var origins = configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
        services.AddCors(options => options.AddPolicy("Frontend", policy =>
            policy.SetIsOriginAllowed(origin =>
                    Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                    (uri.IsLoopback || origins.Contains(origin, StringComparer.OrdinalIgnoreCase)))
                .AllowAnyHeader()
                .AllowAnyMethod()));

        // Limitador de peticiones nativo de ASP.NET Core, por persona (del token).
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, cancellationToken) => new ValueTask(
                context.HttpContext.Response.WriteAsJsonAsync(
                    new { message = "Enviaste demasiados mensajes seguidos. Espera un minuto e inténtalo de nuevo." },
                    cancellationToken));
            // Ventana fija particionada por persona: cada usuario tiene su propio contador.
            options.AddPolicy(ContactPolicy, http => RateLimitPartition.GetFixedWindowLimiter(
                http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonimo",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = ContactPermitsPerMinute,
                    Window = TimeSpan.FromMinutes(1)
                }));
        });

        return services;
    }
}
