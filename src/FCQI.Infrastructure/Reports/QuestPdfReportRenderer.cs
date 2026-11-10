using System.Globalization;
using FCQI.Application.Interfaces;
using FCQI.Application.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FCQI.Infrastructure.Reports;

/// <summary>
/// Adaptador de <see cref="IReportRenderer"/> sobre QuestPDF (licencia
/// Community: uso académico). Los tres documentos comparten encabezado
/// institucional, pie de página y estilo de tablas.
/// </summary>
public class QuestPdfReportRenderer : IReportRenderer
{
    private const string Green = "#00723F";
    private const string GreenDeep = "#024731";
    private const string GoldText = "#875C0B";
    private static readonly CultureInfo Spanish = new("es-MX");

    static QuestPdfReportRenderer() => QuestPDF.Settings.License = LicenseType.Community;

    public byte[] Session(SessionReport report) => Render(
        "Reporte de asesoría", $"Asesoría #{report.Session.Id}, ciclo {report.Session.Term}", report.GeneratedAt,
        col =>
        {
            var s = report.Session;
            KeyValues(col, "Datos de la asesoría",
            [
                ("Materia", $"{s.SubjectName} ({s.SubjectCode})"),
                ("Fecha", $"{s.ScheduledAt.ToString("dddd d 'de' MMMM 'de' yyyy", Spanish)}, {s.ScheduledAt:HH:mm}–{s.EndsAt:HH:mm}"),
                ("Asesor", $"{s.AdvisorName} ({s.AdvisorKind}), {s.AdvisorEmail}"),
                ("Alumno", $"{s.StudentName}, matrícula {s.StudentNumber}, {s.StudentEmail}"),
                ("Programa del alumno", s.StudentProgram),
                ("Modalidad y lugar", $"{s.Modality}, {s.Location}"),
                ("Enlace de reunión", s.MeetingLink ?? "—"),
                ("Tema", s.Topic),
                ("Estado", s.Status),
                ("Solicitada", s.CreatedAt.ToString("dd/MM/yyyy HH:mm"))
            ]);

            KeyValues(col, "Bitácora",
            [
                ("Asistencia", s.Attended switch { true => "Asistió", false => "No asistió", null => "Sin registrar" }),
                ("Notas de avance", s.Notes ?? "—"),
                ("Horas trabajadas", s.HoursWorked.ToString("0.##"))
            ]);

            Section(col, "Historial de estados");
            Table(col, ["Fecha", "De", "A", "Por"],
                report.History.Select(h => new[]
                {
                    h.ChangedAt.ToString("dd/MM/yyyy HH:mm"), h.From ?? "Creación", h.To, h.ChangedBy ?? "Sistema"
                }), [2, 2, 2, 3]);
        });

    public byte[] AdvisorSummary(AdvisorSummary summary) => Render(
        "Resumen de asesorías y horas trabajadas", $"{summary.AdvisorName} ({summary.AdvisorKind}), {summary.Period.Label}",
        summary.GeneratedAt,
        col =>
        {
            col.Item().Text(
                $"Se hace constar que {summary.AdvisorName} impartió {summary.Metrics.Hours:0.##} horas de asesoría " +
                $"en {summary.Metrics.Attended} sesiones con asistencia del alumno, {summary.Period.Label}.");
            Metrics(col, summary.Metrics);
            Breakdown(col, "Por materia", summary.Metrics.BySubject);
            Breakdown(col, "Por día de la semana", summary.Metrics.ByWeekday);
            SessionsTable(col, summary.Sessions, includeAdvisor: false);
        });

    public byte[] Sessions(SessionsExport export) => Render(
        "Reporte de todas las asesorías", $"Creadas {export.Period.Label}", export.GeneratedAt,
        col =>
        {
            Metrics(col, export.Metrics);
            Section(col, "Por asesor");
            Table(col, ["Asesor", "Tipo", "Solicitadas", "Realizadas", "Con asistencia", "Horas"],
                export.Metrics.ByAdvisor.Select(a => new[]
                {
                    a.AdvisorName, a.Kind, a.Requested.ToString(), a.Completed.ToString(), a.Attended.ToString(), a.Hours.ToString("0.##")
                }), [4, 2, 1.5f, 1.5f, 1.5f, 1]);
            Breakdown(col, "Demanda por materia", export.Metrics.BySubject);
            Breakdown(col, "Por día de la semana", export.Metrics.ByWeekday);
            Breakdown(col, "Por modalidad", export.Metrics.ByModality);
            SessionsTable(col, export.Sessions, includeAdvisor: true);
        });

    private static byte[] Render(string title, string subtitle, DateTime generatedAt, Action<ColumnDescriptor> body)
        => Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.Letter);
            page.Margin(1.6f, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontSize(9.5f).FontColor("#1A1A1A"));

            page.Header().Column(col =>
            {
                col.Item().Text("Universidad Autónoma de Baja California").FontSize(9).FontColor(GreenDeep);
                col.Item().Text("Facultad de Ciencias Químicas e Ingeniería").FontSize(13).SemiBold().FontColor(Green);
                col.Item().Text("Programa de Asesorías Académicas").FontSize(9).FontColor(GoldText);
                col.Item().PaddingTop(10).Text(title).FontSize(16).Bold().FontColor(GreenDeep);
                col.Item().Text(subtitle).FontSize(10);
                col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Green);
            });

            page.Content().PaddingVertical(10).Column(col =>
            {
                col.Spacing(8);
                body(col);
            });

            page.Footer().Row(row =>
            {
                row.RelativeItem().Text($"Generado el {generatedAt:dd/MM/yyyy HH:mm} (hora del campus)")
                    .FontSize(8).FontColor(Colors.Grey.Darken1);
                row.AutoItem().Text(t =>
                {
                    t.DefaultTextStyle(s => s.FontSize(8).FontColor(Colors.Grey.Darken1));
                    t.Span("Página ");
                    t.CurrentPageNumber();
                    t.Span(" de ");
                    t.TotalPages();
                });
            });
        })).GeneratePdf();

    private static void Section(ColumnDescriptor col, string title)
        => col.Item().PaddingTop(6).Text(title).FontSize(11.5f).SemiBold().FontColor(GreenDeep);

    private static void KeyValues(ColumnDescriptor col, string title, IEnumerable<(string Key, string Value)> rows)
    {
        Section(col, title);
        col.Item().Table(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.RelativeColumn(1.4f);
                c.RelativeColumn(4);
            });
            foreach (var (key, value) in rows)
            {
                t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(key).SemiBold();
                t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(value);
            }
        });
    }

    private static void Metrics(ColumnDescriptor col, ReportMetrics m)
    {
        if (!m.HasData)
        {
            col.Item().Text("No hay asesorías en el periodo seleccionado.").Italic();
            return;
        }

        KeyValues(col, "Métricas",
        [
            ("Asesorías solicitadas", m.Total.ToString()),
            ("Pendientes / confirmadas", $"{m.Pending} / {m.Confirmed}"),
            ("Realizadas", m.Completed.ToString()),
            ("Con asistencia / inasistencia", $"{m.Attended} / {m.NoShow} ({m.AttendanceRate:0.#} % de asistencia)"),
            ("Canceladas", $"{m.Cancelled} ({m.CancellationRate:0.#} %)"),
            ("Rechazadas", $"{m.Rejected} ({m.RejectionRate:0.#} %)"),
            ("Horas trabajadas", m.Hours.ToString("0.##")),
            ("Anticipación promedio de la solicitud", $"{m.AverageLeadDays:0.#} días")
        ]);
    }

    private static void Breakdown(ColumnDescriptor col, string title, IReadOnlyList<CountRow> rows)
    {
        if (rows.Count == 0) return;
        Section(col, title);
        Table(col, ["", "Asesorías", "Horas"],
            rows.Select(r => new[] { r.Name, r.Count.ToString(), r.Hours.ToString("0.##") }), [4, 1, 1]);
    }

    private static void SessionsTable(ColumnDescriptor col, IReadOnlyList<SessionRecord> rows, bool includeAdvisor)
    {
        if (rows.Count == 0) return;
        Section(col, $"Detalle de asesorías ({rows.Count})");
        string[] header = includeAdvisor
            ? ["#", "Fecha", "Materia", "Asesor", "Alumno", "Estado", "Asistió", "Horas"]
            : ["#", "Fecha", "Materia", "Alumno", "Modalidad", "Estado", "Asistió", "Horas"];
        float[] widths = [0.6f, 1.6f, 2.4f, 2.2f, 2.2f, 1.2f, 0.9f, 0.7f];

        Table(col, header, rows.Select(r => new[]
        {
            r.Id.ToString(), r.ScheduledAt.ToString("dd/MM/yy HH:mm"), r.SubjectName,
            includeAdvisor ? r.AdvisorName : r.StudentName,
            includeAdvisor ? r.StudentName : r.Modality,
            r.Status, r.Attended switch { true => "Sí", false => "No", null => "—" }, r.HoursWorked.ToString("0.##")
        }), widths);
    }

    private static void Table(ColumnDescriptor col, string[] header, IEnumerable<string[]> rows, float[] widths)
        => col.Item().Table(t =>
        {
            t.ColumnsDefinition(c =>
            {
                foreach (var w in widths) c.RelativeColumn(w);
            });
            t.Header(h =>
            {
                foreach (var name in header)
                {
                    h.Cell().Background(GreenDeep).Padding(3).Text(name).FontColor(Colors.White).SemiBold();
                }
            });
            var odd = false;
            foreach (var row in rows)
            {
                string background = (odd = !odd) ? "#FFFFFF" : "#F7F4EE";
                foreach (var cell in row)
                {
                    t.Cell().Background(background).Padding(3).Text(cell);
                }
            }
        });
}
