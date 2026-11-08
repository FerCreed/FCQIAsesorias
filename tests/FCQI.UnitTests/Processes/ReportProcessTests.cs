using FCQI.Application.Administration;
using FCQI.Application.Common;
using FCQI.Domain;
using FCQI.Domain.Entities;
using static FCQI.UnitTests.Processes.ProcessWorld;

namespace FCQI.UnitTests.Processes;

/// <summary>BPMN 6: métricas y constancia de horas.</summary>
public class ReportProcessTests
{
    [Fact]
    public async Task Horas_SoloCuentanLasRealizadasConAsistencia()
    {
        await using var db = await SeedAsync(SessionStatusIds.Completed);
        db.SessionLogs.Add(new SessionLog { SessionId = 1, Attended = true, RecordedBy = Advisor, RecordedAt = Now });
        db.AdvisorySessions.Add(new AdvisorySession
        {
            Id = 2, TermId = 1, AvailabilityId = VirtualBlock, AdvisorId = Advisor, StudentId = Student,
            SubjectId = 1, ScheduledAt = Now, StatusId = SessionStatusIds.Cancelled, Topic = "x"
        });
        await db.SaveChangesAsync();

        var report = await new AdvisoryReportQueryHandler(db, new CurrentTerm(db)).HandleAsync(null, null);

        Assert.Equal(2, report.Requested);
        Assert.Equal(1, report.Completed);
        Assert.Equal(1, report.Cancelled);
        Assert.Equal(100, report.AttendanceRate);
        Assert.Equal(2, report.Hours);
        var row = Assert.Single(report.ByAdvisor);
        Assert.Equal(2, row.Hours);

        var csv = AdvisoryReportQueryHandler.ToCsv(report);
        Assert.Contains("\"Felipe Prueba\",2,1,1,2", csv);
    }
}
