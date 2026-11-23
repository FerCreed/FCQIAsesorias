using FCQI.Application.Reports;

namespace FCQI.Application.Interfaces;

// Genera los documentos PDF de los reportes. Puerto: la infraestructura usa
// QuestPDF, pero la aplicación no depende de ninguna biblioteca de PDF.
public interface IReportRenderer
{
    // PDF del comprobante de una asesoría con su historial de estados.
    byte[] Session(SessionReport report);

    // PDF del resumen de un asesor en un periodo.
    byte[] AdvisorSummary(AdvisorSummary summary);

    // PDF de todas las asesorías del periodo con sus métricas, para dirección.
    byte[] Sessions(SessionsExport export);
}
