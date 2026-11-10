using FCQI.Application.Sessions.Events;
using FCQI.Domain;
using FCQI.Domain.Entities;
using static FCQI.UnitTests.Processes.ProcessWorld;

namespace FCQI.UnitTests.Processes;

/// <summary>El historial de estados registra quién hizo cada cambio (reporte individual).</summary>
public class StatusHistoryAuthorTests
{
    [Fact]
    public async Task AnotaAlAutor_EnLaFilaQueInsertoElTrigger()
    {
        await using var db = await SeedAsync(SessionStatusIds.Confirmed);
        // Lo que dejan los triggers de MySQL: filas sin autor.
        db.SessionStatusChanges.AddRange(
            new SessionStatusChange { Id = 1, SessionId = 1, ToStatusId = SessionStatusIds.Pending, ChangedAt = Now.AddDays(-2) },
            new SessionStatusChange { Id = 2, SessionId = 1, FromStatusId = SessionStatusIds.Pending, ToStatusId = SessionStatusIds.Confirmed, ChangedAt = Now });
        await db.SaveChangesAsync();

        await new StatusHistoryAuthor(db).OnChangedAsync(
            new SessionChanged(1, SessionStatusIds.Pending, SessionStatusIds.Confirmed, Advisor), default);

        Assert.Equal(Advisor, db.SessionStatusChanges.Single(h => h.Id == 2).ChangedBy);
        Assert.Null(db.SessionStatusChanges.Single(h => h.Id == 1).ChangedBy);
    }
}
