using FCQI.Application.Common;
using FCQI.Application.Sessions.Commands;
using FCQI.Application.Sessions.Dtos;
using FCQI.Application.Sessions.Events;
using FCQI.Application.Sessions.Queries;
using FCQI.Domain;
using FCQI.Infrastructure.Messaging;
using static FCQI.UnitTests.Processes.ProcessWorld;

namespace FCQI.UnitTests.Processes;

/// <summary>BPMN 9 (bitácora), 10 (enlace), 11 (cancelar/reprogramar) y 12 (calendario).</summary>
public class SessionProcessTests
{
    [Fact]
    public async Task Alumno_CancelaUnaConfirmada_DentroDelPlazo()
    {
        await using var db = await SeedAsync(SessionStatusIds.Confirmed);

        await TestServices.UpdateStatus(db, Clock()).HandleAsync(1, SessionStatuses.Cancelled, AsStudent());

        Assert.Equal(SessionStatusIds.Cancelled, db.AdvisorySessions.Single().StatusId);
    }

    [Fact]
    public async Task Alumno_NoCancelaAMenosDe24Horas_PeroElAsesorSi()
    {
        await using var db = await SeedAsync(SessionStatusIds.Confirmed);
        var clock = Clock(WednesdaySession.AddHours(-3));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => TestServices.UpdateStatus(db, clock).HandleAsync(1, SessionStatuses.Cancelled, AsStudent()));
        Assert.Contains("24 horas", ex.Message);

        await TestServices.UpdateStatus(db, clock).HandleAsync(1, SessionStatuses.Cancelled, AsAdvisor());
        Assert.Equal(SessionStatusIds.Cancelled, db.AdvisorySessions.Single().StatusId);
    }

    [Fact]
    public async Task CadaCambio_AvisaALosObservadores()
    {
        await using var db = await SeedAsync();
        var observer = new RecordingObserver();

        await TestServices.UpdateStatus(db, Clock(), TestServices.Events(observer))
            .HandleAsync(1, SessionStatuses.Confirmed, AsAdvisor());

        var change = Assert.Single(observer.Changes);
        Assert.Equal(new SessionChanged(1, SessionStatusIds.Pending, SessionStatusIds.Confirmed, Advisor), change);
    }

    [Fact]
    public async Task AlConfirmarUnaVirtual_SeGuardaElEnlace()
    {
        await using var db = await SeedAsync(block: VirtualBlock);
        var events = TestServices.Events(new MeetingLinkObserver(db, new LocationMeetingLinkProvider()));

        await TestServices.UpdateStatus(db, Clock(), events).HandleAsync(1, SessionStatuses.Confirmed, AsAdvisor());

        Assert.Equal(RoomLink, db.AdvisorySessions.Single().MeetingLink);
    }

    [Fact]
    public async Task Bitacora_SoloDespuesDeEmpezar_YDejaLaCitaRealizada()
    {
        await using var db = await SeedAsync(SessionStatusIds.Confirmed);
        var request = new SessionLogRequest(true, "Repasamos límites.");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Log(db, Now).HandleAsync(1, request, AsAdvisor()));

        await Log(db, WednesdaySession.AddHours(1)).HandleAsync(1, request, AsAdvisor());

        Assert.Equal(SessionStatusIds.Completed, db.AdvisorySessions.Single().StatusId);
        Assert.True(db.SessionLogs.Single().Attended);
    }

    [Fact]
    public async Task Bitacora_NoLaRegistraElAlumno_NiSobreUnaPendiente()
    {
        await using var pending = await SeedAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Log(pending, WednesdaySession.AddHours(1)).HandleAsync(1, new SessionLogRequest(true, null), AsAdvisor()));

        await using var confirmed = await SeedAsync(SessionStatusIds.Confirmed);
        await Assert.ThrowsAsync<ForbiddenOperationException>(
            () => Log(confirmed, WednesdaySession.AddHours(1)).HandleAsync(1, new SessionLogRequest(true, null), AsStudent()));
    }

    [Fact]
    public async Task Reprogramar_CreaLaNueva_YCancelaLaAnterior()
    {
        await using var db = await SeedAsync(SessionStatusIds.Confirmed);
        var clock = Clock();
        var handler = new RescheduleSessionCommandHandler(
            db, clock, new(), TestServices.Create(db, clock), TestServices.Events());

        var nueva = await handler.HandleAsync(
            1, new RescheduleSessionRequest(InPersonBlock, new DateTime(2026, 10, 14, 12, 0, 0)), AsStudent());

        Assert.Equal(SessionStatuses.Pending, nueva.Status);
        Assert.Equal(SessionStatusIds.Cancelled, db.AdvisorySessions.Single(s => s.Id == 1).StatusId);
    }

    [Fact]
    public async Task Calendario_EsICalendarValido_YSoloParaLosDeLaCita()
    {
        await using var db = await SeedAsync();
        var handler = new SessionCalendarQueryHandler(db);

        var ics = await handler.HandleAsync(1, AsStudent());

        Assert.StartsWith("BEGIN:VCALENDAR", ics);
        Assert.Contains("DTSTART:20261007T190000Z", ics);
        Assert.Contains("DTEND:20261007T210000Z", ics);
        Assert.Contains(@"DESCRIPTION:Límites\; series\, y más", ics);

        await Assert.ThrowsAsync<ForbiddenOperationException>(
            () => handler.HandleAsync(1, new Caller(99, false, false, true)));
    }

    [Fact]
    public async Task Historial_SeparaLoQueVieneDeLoQuePaso()
    {
        await using var db = await SeedAsync();
        var query = new GetSessionsQueryHandler(db, Clock());

        Assert.Single(await query.HandleAsync(null, null, null, AsStudent(), "upcoming"));
        Assert.Empty(await query.HandleAsync(null, null, null, AsStudent(), "history"));
    }

    private static RecordSessionLogCommandHandler Log(Infrastructure.Persistence.AppDbContext db, DateTime now)
        => new(db, Clock(now), TestServices.Events());
}
