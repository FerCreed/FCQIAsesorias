namespace FCQI.Application.Reports;

// Una asesoría con todos sus datos, ya en hora del campus. Es la fila de
// todos los reportes: el individual, el resumen del asesor y la exportación
// completa de dirección. Los datos salen de asesorias, bitacoras_asesoria e
// historial_estados_sesion, nunca de logs.
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
    // Duración programada del bloque, sin importar si la asesoría se impartió.
    public double DurationHours => (EndsAt - ScheduledAt).TotalHours;

    // Horas que cuentan como trabajadas: asesoría realizada con asistencia.
    public double HoursWorked => Attended == true ? DurationHours : 0;
}

// Un cambio de estado ya en hora local, con el nombre de quien lo hizo si se conoce.
public record StatusChangeRecord(DateTime ChangedAt, string? From, string To, string? ChangedBy);

// Reporte individual de una asesoría (comprobante).
public record SessionReport(SessionRecord Session, IReadOnlyList<StatusChangeRecord> History, DateTime GeneratedAt);

// Periodo de un reporte, en fechas locales del campus. To es inclusivo.
public record ReportPeriod(DateOnly From, DateOnly To)
{
    // Texto del periodo para encabezados de reportes, en formato de fecha mexicano.
    public string Label => $"del {From:dd/MM/yyyy} al {To:dd/MM/yyyy}";
}

// Resumen de un asesor: sus asesorías del periodo y sus horas trabajadas.
public record AdvisorSummary(
    string AdvisorName, string AdvisorEmail, string AdvisorKind,
    ReportPeriod Period, ReportMetrics Metrics, IReadOnlyList<SessionRecord> Sessions, DateTime GeneratedAt);

// Todas las asesorías del sistema de un periodo, con sus métricas.
public record SessionsExport(ReportPeriod Period, ReportMetrics Metrics, IReadOnlyList<SessionRecord> Sessions, DateTime GeneratedAt);

// Filtros comunes de los reportes de dirección.
public record ReportFilter(DateOnly From, DateOnly? To = null, int? AdvisorId = null, int? SubjectId = null, string? Status = null);
