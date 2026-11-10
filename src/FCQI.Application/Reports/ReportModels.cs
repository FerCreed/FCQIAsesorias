namespace FCQI.Application.Reports;

/// <summary>
/// Una asesoría con todos sus datos, ya en hora del campus. Es la fila de
/// todos los reportes: el individual, el resumen del asesor y la exportación
/// completa de dirección. Los datos salen de asesorias, bitacoras_asesoria e
/// historial_estados_sesion, nunca de logs.
/// </summary>
public record SessionRecord(
    long Id,
    string Term,
    DateTime ScheduledAt,
    DateTime EndsAt,
    string SubjectCode,
    string SubjectName,
    int AdvisorId,
    string AdvisorName,
    string AdvisorEmail,
    string AdvisorKind,
    int StudentId,
    string StudentName,
    string StudentNumber,
    string StudentEmail,
    string StudentProgram,
    string Modality,
    string Location,
    string Status,
    string Topic,
    string? MeetingLink,
    bool? Attended,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int StatusChanges)
{
    public double DurationHours => (EndsAt - ScheduledAt).TotalHours;

    /// <summary>Horas que cuentan como trabajadas: asesoría realizada con asistencia.</summary>
    public double HoursWorked => Attended == true ? DurationHours : 0;
}

public record StatusChangeRecord(DateTime ChangedAt, string? From, string To, string? ChangedBy);

/// <summary>Reporte individual de una asesoría (comprobante).</summary>
public record SessionReport(SessionRecord Session, IReadOnlyList<StatusChangeRecord> History, DateTime GeneratedAt);

/// <summary>Periodo de un reporte, en fechas locales del campus. <c>To</c> es inclusivo.</summary>
public record ReportPeriod(DateOnly From, DateOnly To)
{
    public string Label => $"del {From:dd/MM/yyyy} al {To:dd/MM/yyyy}";
}

/// <summary>Resumen de un asesor: sus asesorías del periodo y sus horas trabajadas.</summary>
public record AdvisorSummary(
    string AdvisorName, string AdvisorEmail, string AdvisorKind,
    ReportPeriod Period, ReportMetrics Metrics, IReadOnlyList<SessionRecord> Sessions, DateTime GeneratedAt);

/// <summary>Todas las asesorías del sistema de un periodo, con sus métricas.</summary>
public record SessionsExport(ReportPeriod Period, ReportMetrics Metrics, IReadOnlyList<SessionRecord> Sessions, DateTime GeneratedAt);

/// <summary>Filtros comunes de los reportes de dirección.</summary>
public record ReportFilter(DateOnly From, DateOnly? To = null, int? AdvisorId = null, int? SubjectId = null, string? Status = null);
