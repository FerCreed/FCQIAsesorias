using System.Text;
using FCQI.Application.Common;
using FCQI.Application.Reports;
using FCQI.Domain;
using FCQI.Domain.Entities;
using FCQI.Infrastructure.Persistence;
using FCQI.Infrastructure.Reports;
using static FCQI.UnitTests.Processes.ProcessWorld;

namespace FCQI.UnitTests.Processes;

/// <summary>BPMN 6: reportes individual, resumen del asesor y todas las asesorías.</summary>
public class ReportTests
{
    private static readonly DateOnly Oct1 = new(2026, 10, 1);
    private static readonly DateOnly Oct31 = new(2026, 10, 31);

    /// <summary>La cita sembrada queda realizada con asistencia y se agrega otra cancelada.</summary>
    private static async Task<AppDbContext> WorldAsync()
    {
        var db = await SeedAsync(SessionStatusIds.Completed);
        var first = db.AdvisorySessions.Single();
        first.CreatedAt = new DateTime(2026, 10, 2, 18, 0, 0, DateTimeKind.Utc);
        db.SessionLogs.Add(new SessionLog { SessionId = 1, Attended = true, Notes = "=Repasamos límites", RecordedBy = Advisor, RecordedAt = Now });
        db.AdvisorySessions.Add(new AdvisorySession
        {
            Id = 2, TermId = 1, AvailabilityId = VirtualBlock, AdvisorId = Advisor, StudentId = Student, SubjectId = 1,
            ScheduledAt = new DateTime(2026, 10, 9, 17, 0, 0, DateTimeKind.Utc), StatusId = SessionStatusIds.Cancelled,
            Topic = "Series", CreatedAt = new DateTime(2026, 9, 20, 18, 0, 0, DateTimeKind.Utc)
        });
        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task Individual_LoDescarganParticipantesYDireccion_NoUnAjeno()
    {
        await using var db = await WorldAsync();
        var handler = new SessionReportQueryHandler(db, Clock());

        foreach (var caller in new[] { AsStudent(), AsAdvisor(), AsAdmin() })
        {
            var report = await handler.HandleAsync(1, caller);
            Assert.Equal("Cálculo Diferencial", report.Session.SubjectName);
            Assert.True(report.Session.Attended);
        }

        await Assert.ThrowsAsync<ForbiddenOperationException>(() => handler.HandleAsync(1, new Caller(77, false, false, true)));
    }

    [Fact]
    public async Task Resumen_CuentaSoloHorasRealizadasConAsistencia_YSoloElPropioAsesor()
    {
        await using var db = await WorldAsync();
        var handler = new AdvisorSummaryQueryHandler(db, Clock());

        var summary = await handler.HandleAsync(Advisor, Oct1, Oct31, AsAdvisor());

        Assert.Equal(2, summary.Metrics.Total);
        Assert.Equal(1, summary.Metrics.Cancelled);
        Assert.Equal(2, summary.Metrics.Hours);          // miércoles 12:00–14:00, con asistencia
        Assert.Equal(100, summary.Metrics.AttendanceRate);
        Assert.Equal(AdvisorKinds.Professor, summary.AdvisorKind);

        await handler.HandleAsync(Advisor, Oct1, Oct31, AsAdmin());
        await Assert.ThrowsAsync<ForbiddenOperationException>(() => handler.HandleAsync(Advisor, Oct1, Oct31, AsStudent()));
        await Assert.ThrowsAsync<ForbiddenOperationException>(() => handler.HandleAsync(Advisor, Oct1, Oct31, new Caller(5, false, true, false)));
    }

    [Fact]
    public async Task TodasLasAsesorias_SoloDireccion_YFiltraPorFechaDeCreacion()
    {
        await using var db = await WorldAsync();
        var handler = new SessionsExportQueryHandler(db, Clock());

        var desdeOctubre = await handler.HandleAsync(new ReportFilter(Oct1, Oct31), AsAdmin());
        Assert.Equal([1L], desdeOctubre.Sessions.Select(s => s.Id));

        var desdeSeptiembre = await handler.HandleAsync(new ReportFilter(new DateOnly(2026, 9, 1), Oct31), AsAdmin());
        Assert.Equal(2, desdeSeptiembre.Sessions.Count);

        await Assert.ThrowsAsync<ForbiddenOperationException>(() => handler.HandleAsync(new ReportFilter(Oct1), AsAdvisor()));
        await Assert.ThrowsAsync<BusinessRuleException>(() => handler.HandleAsync(new ReportFilter(Oct31, Oct1), AsAdmin()));
    }

    [Fact]
    public async Task Csv_TraeTodasLasColumnas_YNeutralizaFormulas()
    {
        await using var db = await WorldAsync();
        var export = await new SessionsExportQueryHandler(db, Clock()).HandleAsync(new ReportFilter(new DateOnly(2026, 9, 1), Oct31), AsAdmin());

        var lines = ReportCsv.Write(export.Sessions).TrimEnd().Split(Environment.NewLine);

        Assert.Equal(3, lines.Length);
        Assert.Equal(25, lines[0].Split("\",\"").Length);
        Assert.Contains("\"'=Repasamos límites\"", lines.Single(l => l.StartsWith("\"1\"")));
    }

    [Fact]
    public async Task LosTresPdf_SeGeneran()
    {
        await using var db = await WorldAsync();
        var renderer = new QuestPdfReportRenderer();

        var individual = renderer.Session(await new SessionReportQueryHandler(db, Clock()).HandleAsync(1, AsStudent()));
        var resumen = renderer.AdvisorSummary(await new AdvisorSummaryQueryHandler(db, Clock()).HandleAsync(Advisor, Oct1, Oct31, AsAdvisor()));
        var todas = renderer.Sessions(await new SessionsExportQueryHandler(db, Clock()).HandleAsync(new ReportFilter(Oct1, Oct31), AsAdmin()));

        Assert.All(new[] { individual, resumen, todas }, pdf => Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4)));
    }
}
