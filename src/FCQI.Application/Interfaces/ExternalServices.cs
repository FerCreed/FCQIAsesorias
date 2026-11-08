using FCQI.Domain.Entities;

namespace FCQI.Application.Interfaces;

/// <summary>Correo saliente. Puerto: la infraestructura decide si es SMTP o solo registro.</summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

public record EmailMessage(string To, string Subject, string Body, string? ReplyTo = null);

/// <summary>
/// Genera el enlace de una reunión virtual (BPMN 10). Puerto para conectar
/// Google Meet u otro proveedor sin tocar la lógica de las asesorías.
/// Devuelve null si no hay forma de generarlo; el asesor lo comparte a mano.
/// </summary>
public interface IMeetingLinkProvider
{
    Task<string?> CreateLinkAsync(AdvisorySession session, Location location, CancellationToken cancellationToken = default);
}
