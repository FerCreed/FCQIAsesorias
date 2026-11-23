using System.Net;
using System.Net.Mail;
using FCQI.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FCQI.Infrastructure.Messaging;

// Adaptador de IEmailSender sobre SMTP (System.Net.Mail).
// Se activa al definir Smtp:Host; el resto de la configuración:
// Smtp:Port (587), Smtp:User, Smtp:Password, Smtp:From.
public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;

    // Guarda solo la sección Smtp de la configuración.
    public SmtpEmailSender(IConfiguration configuration) => _config = configuration.GetSection("Smtp");

    // Envía el correo abriendo un cliente por mensaje: SmtpClient no es seguro
    // para uso concurrente y el adaptador es singleton.
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        // Sin usuario se envía sin credenciales (relevo interno del campus).
        using var client = new SmtpClient(_config["Host"], _config.GetValue("Port", 587))
        {
            EnableSsl = _config.GetValue("EnableSsl", true),
            Credentials = string.IsNullOrEmpty(_config["User"])
                ? null
                : new NetworkCredential(_config["User"], _config["Password"])
        };

        using var mail = new MailMessage(_config["From"] ?? _config["User"]!, message.To, message.Subject, message.Body);
        if (message.ReplyTo is not null)
        {
            mail.ReplyToList.Add(message.ReplyTo);
        }

        await client.SendMailAsync(mail, cancellationToken);
    }
}

// Adaptador por omisión mientras no haya servidor SMTP: deja el correo en el
// log en vez de perderlo en silencio, así se puede revisar en desarrollo.
public class LogEmailSender : IEmailSender
{
    private readonly ILogger<LogEmailSender> _logger;

    // Recibe el logger donde se registran los correos.
    public LogEmailSender(ILogger<LogEmailSender> logger) => _logger = logger;

    // Registra destinatario y asunto; el cuerpo solo en nivel Debug.
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        // El cuerpo lleva datos personales y mensajes de alumnos: solo en nivel
        // Debug (desarrollo), nunca en el log normal del contenedor.
        _logger.LogInformation("Correo (sin SMTP configurado) para {To}: {Subject}", message.To, message.Subject);
        _logger.LogDebug("Cuerpo del correo para {To}:\n{Body}", message.To, message.Body);
        return Task.CompletedTask;
    }
}

// Adaptador por omisión de IMeetingLinkProvider: usa el enlace
// fijo de la sala virtual si la sede lo trae en lugares.Detalles.
// Para generar una reunión por cita (Google Meet) basta otro adaptador que
// implemente la misma interfaz.
public class LocationMeetingLinkProvider : IMeetingLinkProvider
{
    // Devuelve el enlace de la sede solo si es una URL https absoluta, para no
    // mandar por correo texto libre o enlaces inseguros como si fueran la reunión.
    public Task<string?> CreateLinkAsync(
        Domain.Entities.AdvisorySession session, Domain.Entities.Location location, CancellationToken cancellationToken = default)
        => Task.FromResult(
            Uri.TryCreate(location.Details?.Trim(), UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
                ? uri.ToString()
                : null);
}
