using FCQI.Domain.Entities;

namespace FCQI.Application.Interfaces;

// Correo saliente. Puerto: la infraestructura decide si es SMTP o solo registro.
public interface IEmailSender
{
    // Envía el mensaje; las implementaciones deciden cómo reportar fallos de entrega.
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

// Correo independiente del proveedor. ReplyTo permite que la respuesta
// llegue a una persona distinta del remitente técnico del sistema.
public record EmailMessage(string To, string Subject, string Body, string? ReplyTo = null);

// Genera el enlace de una reunión virtual (BPMN 10). Puerto para conectar
// Google Meet u otro proveedor sin tocar la lógica de las asesorías.
// Devuelve null si no hay forma de generarlo; el asesor lo comparte a mano.
public interface IMeetingLinkProvider
{
    // Crea el enlace para la asesoría en la sede indicada, o null si no aplica.
    Task<string?> CreateLinkAsync(AdvisorySession session, Location location, CancellationToken cancellationToken = default);
}
