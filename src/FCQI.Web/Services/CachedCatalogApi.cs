using FCQI.Web.Models;

namespace FCQI.Web.Services;

// Decorador de ICatalogApi: sedes y ciclo no cambian durante el
// uso de la app, así que se piden una sola vez. Materias, asesores y horarios
// sí cambian (dirección asigna asesores a materias) y pasan directo.
public class CachedCatalogApi : ICatalogApi
{
    private readonly ICatalogApi _inner;
    // Se guarda la Task y no el resultado: dos llamadas simultáneas comparten la misma petición.
    private Task<List<LocationItem>>? _locations;
    private Task<TermItem>? _term;

    // Envuelve la implementación real del catálogo.
    public CachedCatalogApi(ICatalogApi inner) => _inner = inner;

    // Datos que cambian durante el uso: se delegan sin caché.
    public Task<List<SubjectItem>> GetSubjectsAsync() => _inner.GetSubjectsAsync();
    // Datos estables: se piden una vez y se reutilizan.
    public Task<List<LocationItem>> GetLocationsAsync() => _locations = Retry(_locations) ?? _inner.GetLocationsAsync();
    public Task<TermItem> GetTermAsync() => _term = Retry(_term) ?? _inner.GetTermAsync();
    public Task<List<AdvisorItem>> GetAdvisorsAsync(int subjectId) => _inner.GetAdvisorsAsync(subjectId);
    public Task<List<AvailabilityItem>> GetAvailabilitiesAsync(int advisorId) => _inner.GetAvailabilitiesAsync(advisorId);

    // Una petición fallida no se queda en caché: el siguiente intento vuelve a pedirla.
    private static Task<T>? Retry<T>(Task<T>? cached) => cached is { IsFaulted: true } or { IsCanceled: true } ? null : cached;
}
