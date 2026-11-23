using FCQI.Application.Common;
using FCQI.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Advisors.Commands;

// Contactar con el asesor (BPMN 15): el alumno escribe y el sistema lo envía
// por correo institucional, con respuesta directa al alumno.
public class ContactAdvisorCommandHandler
{
    // Longitud máxima del mensaje; evita usar el servicio como relevo de correos masivos.
    public const int MaxLength = 2000;

    private readonly IApplicationDbContext _context;
    private readonly IEmailSender _email;

    // Recibe el contexto de datos y el emisor de correo por inyección; el
    // emisor es una interfaz para cambiar de proveedor sin tocar este caso de uso.
    public ContactAdvisorCommandHandler(IApplicationDbContext context, IEmailSender email)
    {
        _context = context;
        _email = email;
    }

    // Envía el mensaje del alumno al correo del asesor. El asesor nunca ve
    // su dirección expuesta en el catálogo; el alumno recibe la respuesta en
    // su propio correo gracias a ReplyTo.
    public async Task HandleAsync(int advisorId, string message, Caller caller, CancellationToken cancellationToken = default)
    {
        if (!caller.IsStudent)
        {
            throw new ForbiddenOperationException("Solo un alumno puede escribir a un asesor desde aquí.");
        }

        // Se recorta antes de medir para que un mensaje de puros espacios cuente como vacío.
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

        // SingleAsync y no SingleOrDefault: el alumno viene del token, así que debe existir.
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
