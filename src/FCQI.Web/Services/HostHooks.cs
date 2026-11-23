namespace FCQI.Web.Services;

// Funciones que aporta el anfitrión cuando las tiene. El navegador entrega
// descargas y preferencias con JavaScript (ver FCQI.Web.Browser); el
// escritorio las deja nulas y se usan los diálogos de Avalonia.
public static class HostHooks
{
    // (nombre, tipo MIME, contenido).
    public static Action<string, string, byte[]>? Download { get; set; }

    // Lee una preferencia guardada (localStorage en el navegador); nulo si no existe.
    public static Func<string, string?>? GetSetting { get; set; }

    // Guarda una preferencia del usuario en este equipo.
    public static Action<string, string>? SetSetting { get; set; }
}
