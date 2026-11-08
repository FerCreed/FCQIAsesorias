using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace FCQI.Api.Controllers;

/// <summary>Respuestas de archivo de los reportes, con el mismo formato en todos los controladores.</summary>
internal static class ReportResults
{
    public static FileContentResult Pdf(this ControllerBase controller, byte[] pdf, string name)
        => controller.File(pdf, "application/pdf", $"{name}.pdf");

    /// <summary>UTF-8 con BOM: sin él, Excel muestra mal los acentos.</summary>
    public static FileContentResult Csv(this ControllerBase controller, string csv, string name)
        => controller.File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray(),
            "text/csv", $"{name}.csv");
}
