using FCQI.Web.Models;

namespace FCQI.Web.Services;

/// <summary>
/// Decorador de <see cref="ICatalogApi"/>: sedes y ciclo no cambian durante el
/// uso de la app, así que se piden una sola vez. Materias, asesores y horarios
/// sí cambian (dirección asigna asesores a materias) y pasan directo.
/// </summary>
public class CachedCatalogApi : ICatalogApi
{
    private readonly ICatalogApi _inner;
    private Task<List<LocationItem>>? _locations;
    private Task<TermItem>? _term;

    public CachedCatalogApi(ICatalogApi inner) => _inner = inner;

    public Task<List<SubjectItem>> GetSubjectsAsync() => _inner.GetSubjectsAsync();
    public Task<List<LocationItem>> GetLocationsAsync() => _locations = Retry(_locations) ?? _inner.GetLocationsAsync();
    public Task<TermItem> GetTermAsync() => _term = Retry(_term) ?? _inner.GetTermAsync();
    public Task<List<AdvisorItem>> GetAdvisorsAsync(int subjectId) => _inner.GetAdvisorsAsync(subjectId);
    public Task<List<AvailabilityItem>> GetAvailabilitiesAsync(int advisorId) => _inner.GetAvailabilitiesAsync(advisorId);

    /// <summary>Una petición fallida no se queda en caché: el siguiente intento vuelve a pedirla.</summary>
    private static Task<T>? Retry<T>(Task<T>? cached) => cached is { IsFaulted: true } or { IsCanceled: true } ? null : cached;
}
