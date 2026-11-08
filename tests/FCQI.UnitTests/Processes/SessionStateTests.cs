using FCQI.Domain;
using FCQI.Domain.Sessions;

namespace FCQI.UnitTests.Processes;

/// <summary>El ciclo de vida completo de una asesoría, sin base de datos.</summary>
public class SessionStateTests
{
    [Theory]
    [InlineData(SessionStatusIds.Pending, SessionStatusIds.Confirmed, SessionActor.Advisor, true)]
    [InlineData(SessionStatusIds.Pending, SessionStatusIds.Confirmed, SessionActor.Student, false)]
    [InlineData(SessionStatusIds.Pending, SessionStatusIds.Cancelled, SessionActor.Student, true)]
    [InlineData(SessionStatusIds.Confirmed, SessionStatusIds.Cancelled, SessionActor.Student, true)]
    [InlineData(SessionStatusIds.Confirmed, SessionStatusIds.Completed, SessionActor.Advisor, true)]
    [InlineData(SessionStatusIds.Confirmed, SessionStatusIds.Completed, SessionActor.Student, false)]
    [InlineData(SessionStatusIds.Confirmed, SessionStatusIds.Rejected, SessionActor.Admin, false)]
    [InlineData(SessionStatusIds.Pending, SessionStatusIds.Completed, SessionActor.Admin, false)]
    public void Transiciones(byte from, byte to, SessionActor actor, bool allowed)
        => Assert.Equal(allowed, SessionState.From(from).Allows(to, actor));

    [Theory]
    [InlineData(SessionStatusIds.Cancelled)]
    [InlineData(SessionStatusIds.Rejected)]
    [InlineData(SessionStatusIds.Completed)]
    public void LosEstadosFinales_NoLiberanNiOcupanNadaMas(byte final)
    {
        var state = SessionState.From(final);

        Assert.True(state.IsFinal);
        Assert.False(state.OccupiesSeat);
        Assert.DoesNotContain(new byte[] { 1, 2, 3, 4, 5 }, state.CanMoveTo);
    }
}
