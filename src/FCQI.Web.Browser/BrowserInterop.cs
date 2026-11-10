using System.Runtime.InteropServices.JavaScript;

namespace FCQI.Web.Browser;

/// <summary>Funciones definidas en wwwroot/main.js.</summary>
internal static partial class BrowserInterop
{
    [JSImport("globalThis.fcqiDownload")]
    internal static partial void Download(string fileName, string mimeType, string base64);

    [JSImport("globalThis.fcqiGetSetting")]
    internal static partial string? GetSetting(string key);

    [JSImport("globalThis.fcqiSetSetting")]
    internal static partial void SetSetting(string key, string value);
}
