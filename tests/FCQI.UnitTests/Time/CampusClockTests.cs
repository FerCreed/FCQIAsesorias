using FCQI.Infrastructure.Time;

namespace FCQI.UnitTests.Time;

/// <summary>
/// El modelo anterior guardaba hora local en un datetime sin zona, así que las
/// citas que cruzaban el cambio de horario se corrían una hora. Estas pruebas
/// fijan el comportamiento correcto.
/// </summary>
public class CampusClockTests
{
    private readonly CampusClock _clock = new();

    [Fact]
    public void Summer_UsesPacificDaylightTime()
    {
        // Septiembre: Ensenada está en UTC-7.
        var local = new DateTime(2026, 9, 21, 12, 0, 0);
        Assert.Equal(new DateTime(2026, 9, 21, 19, 0, 0), _clock.ToUtc(local));
    }

    [Fact]
    public void Winter_UsesPacificStandardTime()
    {
        // Enero: Ensenada está en UTC-8. El mismo reloj local es una hora
        // distinta en términos absolutos, que es justo lo que el modelo
        // anterior no podía representar.
        var local = new DateTime(2026, 1, 21, 12, 0, 0);
        Assert.Equal(new DateTime(2026, 1, 21, 20, 0, 0), _clock.ToUtc(local));
    }

    [Fact]
    public void SameWallClockTime_MapsToDifferentInstants_AcrossTheDstBoundary()
    {
        var summer = _clock.ToUtc(new DateTime(2026, 9, 21, 12, 0, 0));
        var winter = _clock.ToUtc(new DateTime(2026, 12, 21, 12, 0, 0));

        Assert.Equal(1, (winter.TimeOfDay - summer.TimeOfDay).TotalHours);
    }

    [Fact]
    public void RoundTrip_PreservesTheWallClockTime()
    {
        foreach (var local in new[]
                 {
                     new DateTime(2026, 1, 15, 9, 30, 0),
                     new DateTime(2026, 6, 15, 17, 0, 0),
                     new DateTime(2026, 9, 21, 12, 0, 0),
                     new DateTime(2026, 11, 30, 7, 0, 0)
                 })
        {
            Assert.Equal(local, _clock.ToLocal(_clock.ToUtc(local)));
        }
    }

    [Fact]
    public void SpringForward_NonExistentLocalTime_IsMovedForwardInsteadOfThrowing()
    {
        // El 8 de marzo de 2026 el reloj salta de 02:00 a 03:00: las 02:30
        // locales no existen. ConvertTimeToUtc lanzaría excepción.
        var nonExistent = new DateTime(2026, 3, 8, 2, 30, 0);

        var utc = _clock.ToUtc(nonExistent);

        Assert.Equal(new DateTime(2026, 3, 8, 10, 30, 0), utc);
    }

    [Fact]
    public void ToLocal_MarksTheResultAsLocalWallClock()
    {
        var utc = new DateTime(2026, 9, 21, 19, 0, 0, DateTimeKind.Utc);
        Assert.Equal(new DateTime(2026, 9, 21, 12, 0, 0), _clock.ToLocal(utc));
    }
}
