using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace FCQI.Web.Services;

/// <summary>
/// Adaptador de <see cref="IPlatformServices"/>. En el navegador descarga con
/// JavaScript (<see cref="HostHooks"/>); en escritorio usa el diálogo de
/// guardar y el lanzador de Avalonia.
/// </summary>
public class AvaloniaPlatformServices : IPlatformServices
{
    private readonly Func<TopLevel?> _topLevel;

    public AvaloniaPlatformServices(Func<TopLevel?> topLevel) => _topLevel = topLevel;

    private TopLevel Top => _topLevel() ?? throw new InvalidOperationException("La ventana todavía no está lista.");

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
