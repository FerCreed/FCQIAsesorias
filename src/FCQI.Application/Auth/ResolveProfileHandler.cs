using FCQI.Application.Interfaces;
using FCQI.Domain;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Auth;

/// <summary>
/// Resuelve la identidad y los roles de un correo institucional.
///
/// El modelo anterior consultaba tres tablas en cadena y devolvía el PRIMER
/// acierto, así que una persona que fuera alumna y asesora a la vez —los
/// asesores pares del programa— quedaba atrapada en un solo rol y nunca podía
/// agendar como alumna. Ahora se resuelven todos sus roles y quien llama
/// elige, con el de mayor alcance como predeterminado.
/// </summary>
public class ResolveProfileHandler
{
    private readonly IApplicationDbContext _context;

    public ResolveProfileHandler(IApplicationDbContext context) => _context = context;

    public async Task<AuthResult?> ResolveAsync(string email, string token, CancellationToken cancellationToken = default)
    {
        email = email.Trim().ToLowerInvariant();
        if (!email.EndsWith("@uabc.edu.mx", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Solo se permiten cuentas institucionales @uabc.edu.mx.");
        }

        var person = await _context.People.AsNoTracking()
            .Where(p => p.Email == email && p.IsActive)
            .Select(p => new
            {
                p.Id,
                p.DisplayName,
                p.Email,
                IsAdmin = p.AdminProfile != null && p.AdminProfile.IsActive,
                IsAdvisor = p.AdvisorProfile != null && p.AdvisorProfile.IsActive,
                IsStudent = p.StudentProfile != null && p.StudentProfile.IsActive
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (person is null)
        {
            return null;
        }

        var roles = new List<string>();
        if (person.IsAdmin) roles.Add(UserRoles.Admin);
        if (person.IsAdvisor) roles.Add(UserRoles.Advisor);
        if (person.IsStudent) roles.Add(UserRoles.Student);

        if (roles.Count == 0)
        {
            return null;
        }

        // ProfileId es el Id de la persona: con el modelo nuevo, los perfiles
        // comparten esa clave, así que sirve para los tres roles.
        return new AuthResult(token, roles[0], person.Id, person.DisplayName, person.Email, roles);
    }

    public async Task<IReadOnlyList<DemoProfileDto>> ListDemoProfilesAsync(CancellationToken cancellationToken = default)
    {
        var people = await _context.People.AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => new
            {
                p.Id,
                p.DisplayName,
                p.Email,
                IsAdmin = p.AdminProfile != null && p.AdminProfile.IsActive,
                IsAdvisor = p.AdvisorProfile != null && p.AdvisorProfile.IsActive,
                IsStudent = p.StudentProfile != null && p.StudentProfile.IsActive
            })
            .OrderBy(p => p.DisplayName)
            .ToListAsync(cancellationToken);

        // Una persona con dos roles aparece dos veces, para poder entrar como
        // cualquiera de los dos en el login de demostración.
        var admins = people.Where(p => p.IsAdmin)
            .Select(p => new DemoProfileDto(UserRoles.Admin, p.Id, p.DisplayName, p.Email));
        var advisors = people.Where(p => p.IsAdvisor)
            .Select(p => new DemoProfileDto(UserRoles.Advisor, p.Id, p.DisplayName, p.Email));
        var students = people.Where(p => p.IsStudent)
            .Select(p => new DemoProfileDto(UserRoles.Student, p.Id, p.DisplayName, p.Email));

        return admins.Concat(advisors).Concat(students).ToList();
    }
}
