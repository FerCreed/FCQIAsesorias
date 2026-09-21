using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FCQI.Application.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FCQI.Infrastructure.Auth;

public class JwtTokenFactory
{
    private readonly IConfiguration _configuration;

    public JwtTokenFactory(IConfiguration configuration) => _configuration = configuration;

    public string Create(AuthResult profile)
    {
        var key = _configuration["Authentication:Jwt:Key"]
                  ?? throw new InvalidOperationException("Falta Authentication:Jwt:Key.");
        var issuer = _configuration["Authentication:Jwt:Issuer"] ?? "FCQI.Api";
        var audience = _configuration["Authentication:Jwt:Audience"] ?? "FCQI.Web";

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, profile.Email),
            new(ClaimTypes.Name, profile.FullName),
            // Identidad de la persona. Toda comprobación de propiedad ("¿es
            // tuya esta asesoría?") se hace contra esto, nunca contra un id
            // que venga en el cuerpo de la petición.
            new(ClaimTypes.NameIdentifier, profile.ProfileId.ToString()),
            new("profileId", profile.ProfileId.ToString())
        };

        // Un claim de rol POR CADA rol: un asesor par lleva Asesor y Alumno, y
        // puede actuar como cualquiera de los dos sin volver a autenticarse.
        claims.AddRange(profile.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
