using FCQI.Web.Models;

namespace FCQI.Web.Services;

/// <summary>
/// Decorador de <see cref="ICatalogApi"/>: materias y sedes no cambian durante
/// el uso de la app, así que se piden una sola vez aunque varias pantallas
/// (catálogo, dirección, horarios) las necesiten. Asesores y horarios sí cambian
/// y pasan directo.
/// </summary>
public class CachedCatalogApi : ICatalogApi
{
    private readonly ICatalogApi _inner;
    private Task<List<SubjectItem>>? _subjects;
    private Task<List<LocationItem>>? _locations;

    public CachedCatalogApi(ICatalogApi inner) => _inner = inner;

    public Task<List<SubjectItem>> GetSubjectsAsync() => _subjects = Retry(_subjects) ?? _inner.GetSubjectsAsync();
    public Task<List<LocationItem>> GetLocationsAsync() => _locations = Retry(_locations) ?? _inner.GetLocationsAsync();
    public Task<List<AdvisorItem>> GetAdvisorsAsync(int subjectId) => _inner.GetAdvisorsAsync(subjectId);
    public Task<List<AvailabilityItem>> GetAvailabilitiesAsync(int advisorId) => _inner.GetAvailabilitiesAsync(advisorId);

    /// <summary>Una petición fallida no se queda en caché: el siguiente intento vuelve a pedirla.</summary>
    private static Task<T>? Retry<T>(Task<T>? cached) => cached is { IsFaulted: true } or { IsCanceled: true } ? null : cached;
}
