using System.Text;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace FCQI.Web.Services;

/// <summary>
/// Adaptador de <see cref="IPlatformServices"/> sobre las API de Avalonia, que
/// funcionan igual en escritorio (diálogo del sistema) y en el navegador
/// (descarga o selector de archivos del navegador).
/// </summary>
public class AvaloniaPlatformServices : IPlatformServices
{
    private readonly Func<TopLevel?> _topLevel;

    public AvaloniaPlatformServices(Func<TopLevel?> topLevel) => _topLevel = topLevel;

    private TopLevel Top => _topLevel() ?? throw new InvalidOperationException("La ventana todavía no está lista.");

    public async Task<bool> SaveTextAsync(string suggestedName, string content)
    {
        if (!Top.StorageProvider.CanSave)
        {
            throw new InvalidOperationException("Este navegador no permite guardar archivos. Prueba con Chrome, Edge o la app de escritorio.");
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

        // Con BOM para que Excel reconozca los acentos del CSV.
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(content)).ToArray();
        await using var stream = await file.OpenWriteAsync();
        await stream.WriteAsync(bytes);
        return true;
    }

    public async Task OpenUrlAsync(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !await Top.Launcher.LaunchUriAsync(uri))
        {
            throw new InvalidOperationException($"No se pudo abrir {url}.");
        }
    }
}
