using FCQI.Application.Auth;
using FCQI.Application.Interfaces;
using FCQI.Domain;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Auth;

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

        var admin = await _context.Admins.AsNoTracking().SingleOrDefaultAsync(a => a.Email.ToLower() == email, cancellationToken);
        if (admin is not null)
        {
            return new AuthResult(token, UserRoles.Admin, admin.Id, admin.FullName, admin.Email);
        }

        var advisor = await _context.Advisors.AsNoTracking().SingleOrDefaultAsync(a => a.Email.ToLower() == email, cancellationToken);
        if (advisor is not null)
        {
            return new AuthResult(token, UserRoles.Advisor, advisor.Id, advisor.FullName, advisor.Email);
        }

        var student = await _context.Students.AsNoTracking().SingleOrDefaultAsync(s => s.Email.ToLower() == email, cancellationToken);
        if (student is not null)
        {
            return new AuthResult(token, UserRoles.Student, student.Id, student.FullName, student.Email);
        }

        return null;
    }

    public async Task<IReadOnlyList<DemoProfileDto>> ListDemoProfilesAsync(CancellationToken cancellationToken = default)
    {
        var admins = await _context.Admins.AsNoTracking()
            .Select(a => new DemoProfileDto(UserRoles.Admin, a.Id, a.FullName, a.Email))
            .ToListAsync(cancellationToken);
        var advisors = await _context.Advisors.AsNoTracking()
            .OrderBy(a => a.FullName)
            .Select(a => new DemoProfileDto(UserRoles.Advisor, a.Id, a.FullName, a.Email))
            .ToListAsync(cancellationToken);
        var students = await _context.Students.AsNoTracking()
            .OrderBy(s => s.FullName)
            .Select(s => new DemoProfileDto(UserRoles.Student, s.Id, s.FullName, s.Email))
            .ToListAsync(cancellationToken);

        return admins.Concat(advisors).Concat(students).ToList();
    }
}
