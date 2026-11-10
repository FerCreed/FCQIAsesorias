using FCQI.Application.Reports;

namespace FCQI.Application.Interfaces;

/// <summary>
/// Genera los documentos PDF de los reportes. Puerto: la infraestructura usa
/// QuestPDF, pero la aplicación no depende de ninguna biblioteca de PDF.
/// </summary>
public interface IReportRenderer
{
    byte[] Session(SessionReport report);
    byte[] AdvisorSummary(AdvisorSummary summary);
    byte[] Sessions(SessionsExport export);
}
