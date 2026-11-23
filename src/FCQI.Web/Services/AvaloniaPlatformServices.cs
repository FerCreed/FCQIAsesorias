using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace FCQI.Web.Services;

// Adaptador de IPlatformServices. En el navegador descarga con
// JavaScript (HostHooks); en escritorio usa el diálogo de
// guardar y el lanzador de Avalonia.
public class AvaloniaPlatformServices : IPlatformServices
{
    private readonly Func<TopLevel?> _topLevel;

    // Recibe una función y no el TopLevel, porque este aún no existe al construir los servicios.
    public AvaloniaPlatformServices(Func<TopLevel?> topLevel) => _topLevel = topLevel;

    // Resuelve el TopLevel en cada uso; lanza si se pide antes de montar la vista.
    private TopLevel Top => _topLevel() ?? throw new InvalidOperationException("La ventana todavía no está lista.");

    // Descarga directa en navegador; diálogo de guardar en escritorio.
    public async Task<bool> SaveFileAsync(string suggestedName, string mimeType, byte[] content)
    {
        if (HostHooks.Download is { } download)
        {
            download(suggestedName, mimeType, content);
            return true;
        }

        var file = await Top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = suggestedName,
            ShowOverwritePrompt = true
        });
        if (file is null)
        {
            return false;
        }

        await using var stream = await file.OpenWriteAsync();
        await stream.WriteAsync(content);
        return true;
    }

    // Abre la URL con el lanzador del sistema; lanza si es inválida o no se pudo abrir.
    public async Task OpenUrlAsync(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !await Top.Launcher.LaunchUriAsync(uri))
        {
            throw new InvalidOperationException($"No se pudo abrir {url}.");
        }
    }

    // shortcut: en escritorio el zoom no se recuerda entre ejecuciones; si se pide, guardar en un archivo de usuario.
    public string? GetSetting(string key) => HostHooks.GetSetting?.Invoke(key);
    public void SetSetting(string key, string value) => HostHooks.SetSetting?.Invoke(key, value);
}
