using FCQI.Web.Models;
using FCQI.Web.Services;
using FCQI.Web.ViewModels;

namespace FCQI.UnitTests.Client;

/// <summary>Lógica del cliente Avalonia que no necesita interfaz para probarse.</summary>
public class ClientLogicTests
{
    [Theory]
    [InlineData("", "x", "Escribe tu correo")]
    [InlineData("alguien@gmail.com", "x", "Solo se aceptan")]
    [InlineData("a@uabc.edu.mx", "", "contraseña")]
    public void Acceso_ValidaElCorreoInstitucional(string email, string password, string expected)
        => Assert.Contains(expected, LoginViewModel.Validate(email, password));

    [Fact]
    public void Acceso_AceptaCorreoUabc() => Assert.Null(LoginViewModel.Validate(" Ana@UABC.edu.mx ", "x"));

    [Fact]
    public void ProximaFecha_CaeEnElDiaDelBloque_YSaltaLaCitaActual()
    {
        var wednesday = new AvailabilityItem { DayOfWeek = 3, StartTime = "12:00:00" };
        var monday = new DateTime(2026, 10, 5);

        var next = wednesday.NextOccurrence(monday);
        Assert.Equal(new DateTime(2026, 10, 7, 12, 0, 0), next);

        // Reprogramar al mismo bloque no puede devolver la misma cita.
        Assert.Equal(new DateTime(2026, 10, 14, 12, 0, 0), wednesday.NextOccurrence(monday, avoid: next));
    }

    [Fact]
    public async Task CacheDelCatalogo_SedesUnaVez_YReintentaSiFallo_MateriasSiempreFrescas()
    {
        var inner = new CountingCatalog { FailFirst = true };
        var cached = new CachedCatalogApi(inner);

        await Assert.ThrowsAsync<HttpRequestException>(cached.GetLocationsAsync);
        await cached.GetLocationsAsync();
        await cached.GetLocationsAsync();
        Assert.Equal(2, inner.LocationCalls);

        // Dirección puede asignar asesores en cualquier momento: las materias no se guardan.
        await cached.GetSubjectsAsync();
        await cached.GetSubjectsAsync();
        Assert.Equal(2, inner.SubjectCalls);
    }

    private sealed class CountingCatalog : ICatalogApi
    {
        public bool FailFirst { get; set; }
        public int SubjectCalls { get; private set; }
        public int LocationCalls { get; private set; }

        public Task<List<SubjectItem>> GetSubjectsAsync()
        {
            SubjectCalls++;
            return Task.FromResult(new List<SubjectItem>());
        }

        public Task<List<AdvisorItem>> GetAdvisorsAsync(int subjectId) => Task.FromResult(new List<AdvisorItem>());
        public Task<List<AvailabilityItem>> GetAvailabilitiesAsync(int advisorId) => Task.FromResult(new List<AvailabilityItem>());
        public Task<List<LocationItem>> GetLocationsAsync()
        {
            LocationCalls++;
            return LocationCalls == 1 && FailFirst
                ? Task.FromException<List<LocationItem>>(new HttpRequestException("sin red"))
                : Task.FromResult(new List<LocationItem>());
        }
        public Task<TermItem> GetTermAsync() => Task.FromResult(new TermItem());
    }
}
