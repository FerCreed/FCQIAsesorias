namespace FCQI.Web.Services;

/// <summary>
/// Funciones que aporta el anfitrión cuando las tiene. El navegador entrega
/// descargas y preferencias con JavaScript (ver FCQI.Web.Browser); el
/// escritorio las deja nulas y se usan los diálogos de Avalonia.
/// </summary>
public static class HostHooks
{
    /// <summary>(nombre, tipo MIME, contenido).</summary>
    public static Action<string, string, byte[]>? Download { get; set; }

    public static Func<string, string?>? GetSetting { get; set; }
    public static Action<string, string>? SetSetting { get; set; }
}
