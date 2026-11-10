using System.Globalization;
using System.Text;

namespace FCQI.Application.Reports;

/// <summary>
/// CSV con todos los datos de cada asesoría (RFC 4180, separador coma). Se
/// abre en Excel u Hojas de cálculo; el BOM lo agrega quien lo entrega.
/// </summary>
public static class ReportCsv
{
    private static readonly string[] Header =
    [
        "Id", "Ciclo", "Fecha", "Inicio", "Fin", "Clave", "Materia", "Asesor", "Correo del asesor", "Tipo de asesor",
        "Alumno", "Matrícula", "Correo del alumno", "Programa del alumno", "Modalidad", "Lugar", "Estado", "Tema",
        "Enlace de reunión", "Asistió", "Notas de bitácora", "Horas trabajadas", "Solicitada", "Última modificación",
        "Cambios de estado"
    ];

    public static string Write(IEnumerable<SessionRecord> rows)
    {
        var csv = new StringBuilder();
        csv.AppendLine(string.Join(",", Header.Select(Quote)));

        foreach (var r in rows)
        {
            csv.AppendLine(string.Join(",", new[]
            {
                r.Id.ToString(CultureInfo.InvariantCulture), r.Term, r.ScheduledAt.ToString("yyyy-MM-dd"),
                r.ScheduledAt.ToString("HH:mm"), r.EndsAt.ToString("HH:mm"), r.SubjectCode, r.SubjectName,
                r.AdvisorName, r.AdvisorEmail, r.AdvisorKind, r.StudentName, r.StudentNumber, r.StudentEmail,
                r.StudentProgram, r.Modality, r.Location, r.Status, r.Topic, r.MeetingLink ?? "",
                r.Attended switch { true => "Sí", false => "No", null => "" }, r.Notes ?? "",
                r.HoursWorked.ToString("0.##", CultureInfo.InvariantCulture),
                r.CreatedAt.ToString("yyyy-MM-dd HH:mm"), r.UpdatedAt.ToString("yyyy-MM-dd HH:mm"),
                r.StatusChanges.ToString(CultureInfo.InvariantCulture)
            }.Select(Quote)));
        }

        return csv.ToString();
    }

    /// <summary>Entre comillas siempre; las comillas internas se duplican. También neutraliza fórmulas de Excel.</summary>
    private static string Quote(string value)
    {
        var safe = value.Length > 0 && "=+-@".Contains(value[0]) ? "'" + value : value;
        return $"\"{safe.Replace("\"", "\"\"")}\"";
    }
}
