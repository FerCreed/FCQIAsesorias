using FCQI.Application.Common;
using FCQI.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Advisors.Commands;

/// <summary>
/// Contactar con el asesor (BPMN 15): el alumno escribe y el sistema lo envía
/// por correo institucional, con respuesta directa al alumno.
/// </summary>
public class ContactAdvisorCommandHandler
{
    public const int MaxLength = 2000;

    private readonly IApplicationDbContext _context;
    private readonly IEmailSender _email;

    public ContactAdvisorCommandHandler(IApplicationDbContext context, IEmailSender email)
    {
        _context = context;
        _email = email;
    }

    public async Task HandleAsync(int advisorId, string message, Caller caller, CancellationToken cancellationToken = default)
    {
        if (!caller.IsStudent)
        {
            throw new ForbiddenOperationException("Solo un alumno puede escribir a un asesor desde aquí.");
        }

        var text = message?.Trim() ?? string.Empty;
        if (text.Length is 0 or > MaxLength)
        {
            throw new BusinessRuleException($"Escribe un mensaje de 1 a {MaxLength} caracteres.");
        }

        var advisor = await _context.AdvisorProfiles.AsNoTracking()
                          .Where(a => a.PersonId == advisorId && a.IsActive)
                          .Select(a => new { a.Person.Email, a.Person.DisplayName })
                          .SingleOrDefaultAsync(cancellationToken)
                      ?? throw new NotFoundException("El asesor no existe o no está activo.");

        var student = await _context.People.AsNoTracking()
            .Where(p => p.Id == caller.PersonId)
            .Select(p => new { p.Email, p.DisplayName })
            .SingleAsync(cancellationToken);

        await _email.SendAsync(new EmailMessage(
            advisor.Email,
            $"Mensaje de {student.DisplayName} (Programa de Asesorías FCQI)",
            text,
            ReplyTo: student.Email), cancellationToken);
    }
}
