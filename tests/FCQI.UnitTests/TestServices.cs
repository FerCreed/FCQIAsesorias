using FCQI.Application.Common;
using FCQI.Application.Interfaces;
using FCQI.Application.Sessions;
using FCQI.Application.Sessions.Commands;
using FCQI.Application.Sessions.Events;
using FCQI.Infrastructure.Persistence;
using FCQI.Infrastructure.Time;
using Microsoft.Extensions.Logging.Abstractions;

namespace FCQI.UnitTests;

/// <summary>Reloj del campus con "ahora" fijo, para que las pruebas no caduquen.</summary>
internal sealed class TestClock : IClock
{
    private readonly CampusClock _campus = new();

    public TestClock(DateTime? utcNow = null) => UtcNow = utcNow ?? DateTime.UtcNow;

    public TimeZoneInfo Zone => _campus.Zone;
    public DateTime UtcNow { get; set; }
    public DateTime LocalNow => _campus.ToLocal(UtcNow);
    public DateTime ToUtc(DateTime local) => _campus.ToUtc(local);
    public DateTime ToLocal(DateTime utc) => _campus.ToLocal(utc);
}

/// <summary>Observador que solo anota lo que oye.</summary>
internal sealed class RecordingObserver : ISessionObserver
{
    public List<SessionChanged> Changes { get; } = [];

    public Task OnChangedAsync(SessionChanged change, CancellationToken cancellationToken)
    {
        Changes.Add(change);
        return Task.CompletedTask;
    }
}

/// <summary>Construye los handlers con sus dependencias de prueba.</summary>
internal static class TestServices
{
    public static SessionEventPublisher Events(params ISessionObserver[] observers)
        => new(observers, NullLogger<SessionEventPublisher>.Instance);

    public static UpdateSessionStatusCommandHandler UpdateStatus(AppDbContext db, IClock? clock = null, SessionEventPublisher? events = null)
        => new(db, clock ?? new TestClock(), new SessionPolicy(), events ?? Events());

    public static CreateSessionCommandHandler Create(AppDbContext db, IClock? clock = null)
        => new(db, new CurrentTerm(db), clock ?? new TestClock(), Events());
}
