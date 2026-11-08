using System.Text;
using FCQI.Api.Filters;
using FCQI.Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace FCQI.Api.Configuration;

/// <summary>Configuración web de la API: controladores, JWT y CORS.</summary>
public static class ApiSetup
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers(options => options.Filters.Add<ApiExceptionFilter>());
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        // Identidad de quien llama, leída del JWT.
        services.AddHttpContextAccessor();
        services.AddScoped<CurrentUser>();

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

        return services;
    }
}
