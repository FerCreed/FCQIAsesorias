using System.Runtime.InteropServices.JavaScript;

namespace FCQI.Web.Browser;

// Funciones definidas en wwwroot/main.js.
internal static partial class BrowserInterop
{
    // Descarga un archivo en el navegador. El contenido viaja en base64 porque
    // la interoperabilidad con JavaScript transporta cadenas de forma directa.
    [JSImport("globalThis.fcqiDownload")]
    internal static partial void Download(string fileName, string mimeType, string base64);

    // Lee una preferencia de localStorage; nulo si no existe o el navegador lo bloquea.
    [JSImport("globalThis.fcqiGetSetting")]
    internal static partial string? GetSetting(string key);

    // Guarda una preferencia en localStorage; ignora el fallo en modo privado.
    [JSImport("globalThis.fcqiSetSetting")]
    internal static partial void SetSetting(string key, string value);
}
