using FCQI.Application.Advisors.Commands;
using FCQI.Application.Common;
using FCQI.Application.Interfaces;
using FCQI.Application.Sessions.Events;
using FCQI.Domain;
using static FCQI.UnitTests.Processes.ProcessWorld;

namespace FCQI.UnitTests.Processes;

/// <summary>Correo saliente: BPMN 15 (contactar al asesor) y avisos de cambio de estado.</summary>
public class ContactAndNoticeTests
{
    private sealed class Outbox : IEmailSender
    {
        public List<EmailMessage> Sent { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Sent.Add(message);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Alumno_EscribeAlAsesor_YLaRespuestaVaASuCorreo()
    {
        await using var db = await SeedAsync();
        var outbox = new Outbox();

        await new ContactAdvisorCommandHandler(db, outbox).HandleAsync(Advisor, "  ¿Revisamos series?  ", AsStudent());

        var mail = Assert.Single(outbox.Sent);
        Assert.Equal("felipe.marquez@uabc.edu.mx", mail.To);
        Assert.Equal("yesua.diaz@uabc.edu.mx", mail.ReplyTo);
        Assert.Equal("¿Revisamos series?", mail.Body);
    }

    [Fact]
    public async Task Contacto_RechazaAlQueNoEsAlumno_MensajesVacios_YAsesoresInexistentes()
    {
        await using var db = await SeedAsync();
        var outbox = new Outbox();
        var handler = new ContactAdvisorCommandHandler(db, outbox);

        await Assert.ThrowsAsync<ForbiddenOperationException>(() => handler.HandleAsync(Advisor, "Hola", AsAdvisor()));
        await Assert.ThrowsAsync<BusinessRuleException>(() => handler.HandleAsync(Advisor, "   ", AsStudent()));
        await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.HandleAsync(Advisor, new string('x', ContactAdvisorCommandHandler.MaxLength + 1), AsStudent()));
        await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(404, "Hola", AsStudent()));
        Assert.Empty(outbox.Sent);
    }

    [Theory]
    [InlineData(Advisor, new[] { "yesua.diaz@uabc.edu.mx" })]
    [InlineData(Student, new[] { "felipe.marquez@uabc.edu.mx" })]
    [InlineData(ProcessWorld.Admin, new[] { "yesua.diaz@uabc.edu.mx", "felipe.marquez@uabc.edu.mx" })]
    public async Task Aviso_LlegaALaOtraParte_YNombraAQuienActuo(int actor, string[] expected)
    {
        await using var db = await SeedAsync();
        var outbox = new Outbox();

        await new SessionNotifier(db, outbox, Clock())
            .OnChangedAsync(new SessionChanged(1, SessionStatusIds.Pending, SessionStatusIds.Cancelled, actor), default);

        Assert.Equal(expected, outbox.Sent.Select(m => m.To));
        var who = actor switch { ProcessWorld.Advisor => "Felipe", ProcessWorld.Student => "Yesua", _ => "Directora" };
        Assert.All(outbox.Sent, m => Assert.StartsWith(who, m.Body));
    }
}
