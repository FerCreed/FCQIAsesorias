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

        var token = new JwtSecurityToken(
            issuer,
            audience,
            [
                new Claim(ClaimTypes.Role, profile.Role),
                new Claim(ClaimTypes.Email, profile.Email),
                new Claim(ClaimTypes.Name, profile.FullName),
                new Claim("profileId", profile.ProfileId.ToString())
            ],
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
