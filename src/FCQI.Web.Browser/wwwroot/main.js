// Versión de esta construcción (?v= que index.html pone a main.js). Se añade a
// todos los archivos de .NET: se llaman igual en cada versión y, sin esto, el
// navegador o Cloudflare servirían los de la versión anterior durante horas.
const version = new URL(import.meta.url).searchParams.get('v') ?? '';
const withVersion = url => version ? `${url}${url.includes('?') ? '&' : '?'}v=${version}` : url;
const { dotnet } = await import(withVersion('./_framework/dotnet.js'));

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error("Expected to be running in a browser");

// Descarga de archivos (reportes PDF/CSV, calendario .ics). La invoca
// BrowserInterop.Download. Funciona en todos los navegadores, a diferencia del
// selector de archivos de File System Access, que solo existe en Chromium.
globalThis.fcqiDownload = (fileName, mimeType, base64) => {
    const bytes = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
    const url = URL.createObjectURL(new Blob([bytes], { type: mimeType }));
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    setTimeout(() => URL.revokeObjectURL(url), 10000);
};

// Preferencias de esta persona en este equipo (p. ej. el zoom).
globalThis.fcqiGetSetting = key => { try { return localStorage.getItem(key); } catch { return null; } };
globalThis.fcqiSetSetting = (key, value) => { try { localStorage.setItem(key, value); } catch { /* modo privado */ } };

try {
    const dotnetRuntime = await dotnet
        .withApplicationArgumentsFromQuery()
        .withResourceLoader((type, name, defaultUri) => withVersion(defaultUri))
        .create();

    const config = dotnetRuntime.getConfig();

    await dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);
} catch (error) {
    // Sin esto, un fallo al arrancar (navegador viejo, red cortada) deja la pantalla vacía.
    console.error(error);
    document.getElementById('out').innerHTML =
        '<div class="avalonia-splash" role="alert" style="flex-direction:column;padding:24px;text-align:center">' +
        '<p>No se pudo abrir la aplicación en este navegador.</p>' +
        '<p>Actualiza el navegador (Chrome, Safari o Firefox recientes) y recarga la página.</p></div>';
}
