using System.Net;
using System.Net.Mail;
using FCQI.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FCQI.Infrastructure.Messaging;

/// <summary>
/// Adaptador de <see cref="IEmailSender"/> sobre SMTP (System.Net.Mail).
/// Se activa al definir <c>Smtp:Host</c>; el resto de la configuración:
/// <c>Smtp:Port</c> (587), <c>Smtp:User</c>, <c>Smtp:Password</c>, <c>Smtp:From</c>.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;

    public SmtpEmailSender(IConfiguration configuration) => _config = configuration.GetSection("Smtp");

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
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

/// <summary>
/// Adaptador por omisión mientras no haya servidor SMTP: deja el correo en el
/// log en vez de perderlo en silencio, así se puede revisar en desarrollo.
/// </summary>
public class LogEmailSender : IEmailSender
{
    private readonly ILogger<LogEmailSender> _logger;

    public LogEmailSender(ILogger<LogEmailSender> logger) => _logger = logger;

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Correo (sin SMTP configurado) para {To}: {Subject}\n{Body}",
            message.To, message.Subject, message.Body);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Adaptador por omisión de <see cref="IMeetingLinkProvider"/>: usa el enlace
/// fijo de la sala virtual si la sede lo trae en <c>lugares.Detalles</c>.
/// Para generar una reunión por cita (Google Meet) basta otro adaptador que
/// implemente la misma interfaz.
/// </summary>
public class LocationMeetingLinkProvider : IMeetingLinkProvider
{
    public Task<string?> CreateLinkAsync(
        Domain.Entities.AdvisorySession session, Domain.Entities.Location location, CancellationToken cancellationToken = default)
        => Task.FromResult(
            Uri.TryCreate(location.Details?.Trim(), UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
                ? uri.ToString()
                : null);
}
