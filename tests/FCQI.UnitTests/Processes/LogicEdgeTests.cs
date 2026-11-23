using FCQI.Application.Common;
using FCQI.Application.Reports;
using FCQI.Application.Sessions.Dtos;
using FCQI.Domain;
using static FCQI.UnitTests.Processes.ProcessWorld;

namespace FCQI.UnitTests.Processes;

// Pruebas de borde: cada caso es un error de lógica concreto que estas reglas deben impedir.
public class LogicEdgeTests
{
    // "Ahora" es lunes 5/oct/2026 12:00 en Ensenada; el bloque virtual es viernes 10:00–12:00.
    // Cada fecha rompe una sola regla de la solicitud; la última es válida y sirve de control,
    // para que una regla demasiado estricta no pase desapercibida.
    [Theory]
    [InlineData("2026-10-08 10:00", "La fecha elegida es jueves")]          // día distinto al del bloque
    [InlineData("2026-10-09 11:00", "empieza a las 10:00")]                  // hora distinta al inicio
    [InlineData("2026-10-02 10:00", "en el pasado")]                         // viernes ya pasado
    [InlineData("2026-12-18 10:00", "fuera del ciclo 2026-2")]               // viernes después del ciclo
    [InlineData("2026-10-09 10:00", null)]                                   // correcto
    public async Task Solicitud_SoloSeAceptaEnElDiaHoraYCicloDelBloque(string local, string? error)
    {
        await using var db = await SeedAsync();
        var request = new CreateSessionRequest(Student, Advisor, 1, VirtualBlock, DateTime.Parse(local), "Series");
        var create = () => TestServices.Create(db, Clock()).HandleAsync(request, AsStudent());

        if (error is null)
        {
            Assert.Equal(SessionStatuses.Pending, (await create()).Status);
            return;
        }

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(create);
        Assert.Contains(error, ex.Message);
    }

    // Las métricas de dirección: horas solo con asistencia, porcentajes sobre la base correcta,
    // semana que empieza en lunes y ninguna división entre cero cuando no hay datos.
    [Fact]
    public void Metricas_CuentanHorasConAsistencia_YNoDividenEntreCero()
    {
        var monday = new DateTime(2026, 10, 5, 10, 0, 0);
        var rows = new[]
        {
            Row(SessionStatuses.Completed, true, monday, hours: 2),             // cuenta 2 h
            Row(SessionStatuses.Completed, false, monday.AddDays(-1), hours: 1), // domingo, no asistió: 0 h
            Row(SessionStatuses.Cancelled, null, monday.AddDays(7), hours: 1),
            Row(SessionStatuses.Pending, null, monday.AddDays(14), hours: 1)
        };

        var m = ReportMetrics.From(rows);

        Assert.Equal((4, 2, 1, 1), (m.Total, m.Completed, m.Attended, m.NoShow));
        Assert.Equal(50, m.AttendanceRate);      // 1 de 2 realizadas, no de 4 solicitadas
        Assert.Equal(25, m.CancellationRate);    // 1 de 4 solicitadas
        Assert.Equal(2, m.Hours);
        Assert.Equal(2, m.AverageLeadDays);
        Assert.Equal(["Lunes", "Domingo"], m.ByWeekday.Select(d => d.Name));
        Assert.Equal([3, 1], m.ByWeekday.Select(d => d.Count));

        var empty = ReportMetrics.From([]);
        Assert.False(empty.HasData);
        Assert.Equal((0d, 0d, 0d), (empty.AttendanceRate, empty.CancellationRate, empty.AverageLeadDays));
    }

    // Fila mínima de reporte; se solicitó dos días antes de la cita.
    private static SessionRecord Row(string status, bool? attended, DateTime start, double hours)
        => new(1, "2026-2", start, start.AddHours(hours), "CALCULO", "Cálculo Diferencial",
            Advisor, "Felipe Prueba", "felipe@uabc.edu.mx", AdvisorKinds.Professor,
            Student, "Yesua Prueba", "2208134", "yesua@uabc.edu.mx", "Ingeniería Química",
            Modalities.InPerson, "Cubículo FCQI", status, "Tema", null, attended, null,
            start.AddDays(-2), start.AddDays(-2), 1);
}
