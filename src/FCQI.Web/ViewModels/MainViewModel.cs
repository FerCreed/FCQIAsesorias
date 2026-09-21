using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FCQI.Web.Models;
using FCQI.Web.Serialization;
using FCQI.Web.Services;

namespace FCQI.Web.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly FcqiApiClient _api = new();

    [ObservableProperty] private string _page = "login";
    [ObservableProperty] private string _status = "Elige un perfil institucional de demo (OAuth Google se activa con ClientId).";
    [ObservableProperty] private AuthResultItem? _session;

    /// <summary>
    /// Rol con el que se está usando el sistema AHORA.
    ///
    /// El token trae todos los roles de la persona, así que cambiar de uno a
    /// otro es cosa de la interfaz y no exige volver a entrar. Antes esta
    /// pantalla leía <c>Session.Role</c>, que es solo el predeterminado: un
    /// asesor par entraba siempre como asesor y, para agendar como alumno,
    /// tenía que salir y volver a entrar con el otro perfil.
    /// </summary>
    [ObservableProperty] private string? _activeRole;

    [ObservableProperty] private DemoProfileItem? _selectedProfile;
    [ObservableProperty] private SubjectItem? _selectedSubject;
    [ObservableProperty] private AdvisorItem? _selectedAdvisor;
    [ObservableProperty] private AvailabilityItem? _selectedSlot;
    [ObservableProperty] private SessionItem? _selectedSession;
    [ObservableProperty] private AdvisorItem? _selectedAdminAdvisor;
    [ObservableProperty] private string _topic = "Dudas del parcial";
    [ObservableProperty] private string _welcome = "Sistema de Asesorías FCQI";
    [ObservableProperty] private bool _googleReady;

    public ObservableCollection<DemoProfileItem> Profiles { get; } = [];

    /// <summary>Roles que la persona puede usar, tal y como vienen del token.</summary>
    public ObservableCollection<string> Roles { get; } = [];

    public ObservableCollection<SubjectItem> Subjects { get; } = [];
    public ObservableCollection<AdvisorItem> Advisors { get; } = [];
    public ObservableCollection<AvailabilityItem> Slots { get; } = [];
    public ObservableCollection<SessionItem> Sessions { get; } = [];
    public ObservableCollection<AdvisorItem> AdminAdvisors { get; } = [];
    public ObservableCollection<AssignableSubject> AdminSubjects { get; } = [];

    public bool IsStudent => ActiveRole == UserRoles.Student;
    public bool IsAdvisor => ActiveRole == UserRoles.Advisor;
    public bool IsAdmin => ActiveRole == UserRoles.Admin;
    public bool IsLoggedIn => Session is not null;

    /// <summary>El selector de rol solo tiene sentido con más de un rol.</summary>
    public bool CanSwitchRole => IsLoggedIn && Roles.Count > 1;

    public bool ShowLogin => !IsLoggedIn;
    public bool ShowHome => IsLoggedIn && Page == "home";
    public bool ShowSearch => IsStudent && Page == "search";
    public bool ShowSessions => IsLoggedIn && Page == "sessions";
    public bool ShowAdmin => IsAdmin && Page == "admin";

    /// <summary>Confirmar o rechazar: el asesor dueño y dirección.</summary>
    public bool CanDecide => IsAdvisor || IsAdmin;

    /// <summary>Darse de baja: el alumno dueño y dirección.</summary>
    public bool CanCancel => IsStudent || IsAdmin;

    public MainViewModel()
    {
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            var config = await _api.GetAuthConfigAsync();
            GoogleReady = config.GoogleConfigured;

            if (GoogleReady)
            {
                // Con OAuth configurado la API retira el directorio de perfiles
                // (devuelve 404), así que pedirlo solo produce un error
                // engañoso: "no se pudo hablar con la API".
                Status = "Google OAuth está configurado: el acceso de demostración queda deshabilitado " +
                         "y el botón de Google todavía no está integrado en esta pantalla. " +
                         "Para probar con perfiles, deja vacío Authentication:Google:ClientId.";
                return;
            }

            var profiles = await _api.GetDemoProfilesAsync();
            Profiles.Clear();
            foreach (var profile in profiles)
            {
                Profiles.Add(profile);
            }

            SelectedProfile = Profiles.FirstOrDefault(p => p.Role == UserRoles.Student);
            Status = "Demo: entra con un perfil. Para OAuth, configura Authentication:Google:ClientId.";
        }
        catch (Exception ex)
        {
            Status = $"No se pudo hablar con la API en {FcqiApiClient.BaseUrl}: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (SelectedProfile is null)
        {
            Status = "Selecciona un perfil.";
            return;
        }

        try
        {
            Session = await _api.DemoLoginAsync(SelectedProfile.Email);

            // A partir de aquí la API exige el token en cada petición.
            _api.UseToken(Session.Token);

            Roles.Clear();
            foreach (var role in Session.Roles)
            {
                Roles.Add(role);
            }

            Welcome = $"¡Bienvenido! {Session.FullName}";

            // El perfil elegido en el selector de demostración decide con qué
            // rol se entra, si la persona lo tiene. El resto de la carga la
            // dispara OnActiveRoleChanged, que es el único sitio donde se
            // decide qué datos corresponden a cada rol.
            ActiveRole = Session.Roles.Contains(SelectedProfile.Role)
                ? SelectedProfile.Role
                : Session.Roles.FirstOrDefault() ?? Session.Role;
        }
        catch (Exception ex)
        {
            Status = $"Login: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Logout()
    {
        _api.UseToken(null);
        Session = null;
        ActiveRole = null;
        Roles.Clear();
        ClearWorkspace();

        // Los datos del catálogo también se van: la siguiente persona los
        // vuelve a pedir con su propio token.
        Subjects.Clear();
        AdminAdvisors.Clear();
        AdminSubjects.Clear();

        Page = "login";
        Welcome = "Sistema de Asesorías FCQI";
        Status = "Sesión cerrada.";
        NotifyNav();
    }

    [RelayCommand]
    private void Go(string page) => Page = page;

    /// <summary>
    /// Page y ActiveRole deciden qué pantalla se ve, pero lo hacen a través de
    /// propiedades calculadas (ShowSearch, ShowSessions…). Sin avisar de esas,
    /// cambiar de página no movía la interfaz: tras solicitar una asesoría, el
    /// alumno se quedaba mirando el buscador.
    /// </summary>
    private void NotifyNav()
    {
        OnPropertyChanged(nameof(IsStudent));
        OnPropertyChanged(nameof(IsAdvisor));
        OnPropertyChanged(nameof(IsAdmin));
        OnPropertyChanged(nameof(IsLoggedIn));
        OnPropertyChanged(nameof(CanSwitchRole));
        OnPropertyChanged(nameof(CanDecide));
        OnPropertyChanged(nameof(CanCancel));
        OnPropertyChanged(nameof(ShowLogin));
        OnPropertyChanged(nameof(ShowHome));
        OnPropertyChanged(nameof(ShowSearch));
        OnPropertyChanged(nameof(ShowSessions));
        OnPropertyChanged(nameof(ShowAdmin));
    }

    partial void OnPageChanged(string value) => NotifyNav();

    partial void OnSessionChanged(AuthResultItem? value) => NotifyNav();

    /// <summary>
    /// Cambiar de rol cambia el sistema entero: lo que el alumno ve no le
    /// sirve al asesor. Se vuelve al inicio, se tira lo que había en pantalla
    /// y se recarga con el rol nuevo.
    /// </summary>
    partial void OnActiveRoleChanged(string? value)
    {
        NotifyNav();

        if (Session is null || string.IsNullOrEmpty(value))
        {
            return;
        }

        ClearWorkspace();
        Page = "home";
        Status = Roles.Count > 1
            ? $"Estás usando el sistema como {value}. Puedes cambiar de rol en el menú superior."
            : $"Sesión {value} · {Session.Email}";

        _ = LoadHomeDataAsync();
    }

    private void ClearWorkspace()
    {
        SelectedSubject = null;
        SelectedAdvisor = null;
        SelectedSlot = null;
        SelectedSession = null;
        Advisors.Clear();
        Slots.Clear();
        Sessions.Clear();
    }

    [RelayCommand]
    private async Task LoadHomeDataAsync()
    {
        if (Session is null)
        {
            return;
        }

        try
        {
            Subjects.Clear();
            foreach (var subject in await _api.GetSubjectsAsync())
            {
                Subjects.Add(subject);
            }

            if (IsAdmin)
            {
                await LoadAdminAsync();
            }

            await RefreshForRoleAsync();
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
    }

    [RelayCommand]
    private async Task LoadAdvisorsAsync()
    {
        if (SelectedSubject is null)
        {
            return;
        }

        try
        {
            Advisors.Clear();
            Slots.Clear();
            foreach (var advisor in await _api.GetAdvisorsAsync(SelectedSubject.Id))
            {
                Advisors.Add(advisor);
            }

            Status = $"{Advisors.Count} tutores asignados por dirección a {SelectedSubject.Name}.";
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
    }

    [RelayCommand]
    private async Task LoadSlotsAsync()
    {
        if (SelectedAdvisor is null)
        {
            return;
        }

        try
        {
            Slots.Clear();
            foreach (var slot in await _api.GetAvailabilitiesAsync(SelectedAdvisor.Id))
            {
                Slots.Add(slot);
            }
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
    }

    [RelayCommand]
    private async Task RequestSessionAsync()
    {
        if (Session is null || SelectedSubject is null || SelectedAdvisor is null || SelectedSlot is null)
        {
            Status = "Elige materia, tutor y horario.";
            return;
        }

        // Un asesor par puede aparecer en su propia lista de tutores. La API
        // lo rechaza, pero conviene decirlo antes de viajar hasta el servidor.
        if (SelectedAdvisor.Id == Session.ProfileId)
        {
            Status = "No puedes agendar una asesoría contigo mismo. Elige otro tutor.";
            return;
        }

        try
        {
            var day = DateTime.Today.AddDays(1);
            while ((int)day.DayOfWeek != SelectedSlot.DayOfWeek)
            {
                day = day.AddDays(1);
            }

            var start = TimeSpan.TryParse(SelectedSlot.StartTime, out var parsed) ? parsed : TimeSpan.FromHours(9);
            await _api.CreateSessionAsync(new CreateSessionBody
            {
                // La API ignora este campo y usa la identidad del token; viaja
                // por compatibilidad con la ventanilla de dirección.
                StudentId = Session.ProfileId,
                AdvisorId = SelectedAdvisor.Id,
                SubjectId = SelectedSubject.Id,
                AvailabilityId = SelectedSlot.Id,
                ScheduledAt = day.Add(start),
                Topic = Topic
            });
            Status = "Solicitud enviada (Pendiente). El tutor la verá en su bandeja.";
            await RefreshForRoleAsync();
            Page = "sessions";
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
    }

    [RelayCommand]
    private Task ConfirmSessionAsync() => ChangeStatusAsync(SessionStatuses.Confirmed, "Solicitud confirmada.");

    [RelayCommand]
    private Task RejectSessionAsync() => ChangeStatusAsync(SessionStatuses.Rejected, "Solicitud rechazada.");

    [RelayCommand]
    private Task CancelSessionAsync() => ChangeStatusAsync(SessionStatuses.Cancelled, "Asesoría cancelada.");

    /// <summary>
    /// Antes estos comandos no atrapaban nada: la API contestaba 403 o 400 y
    /// la excepción se perdía en el vacío, así que el botón parecía no hacer
    /// nada y el motivo no llegaba nunca a la pantalla.
    /// </summary>
    private async Task ChangeStatusAsync(string status, string done)
    {
        if (SelectedSession is null)
        {
            Status = "Elige primero una asesoría de la lista.";
            return;
        }

        try
        {
            await _api.UpdateStatusAsync(SelectedSession.Id, status);
            Status = done;
            await RefreshForRoleAsync();
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
    }

    [RelayCommand]
    private async Task LoadAdminAsync()
    {
        try
        {
            // Guardar el tutor elegido antes de vaciar la lista: al hacer
            // Clear, el ListBox deja la selección en nulo y el panel saltaba
            // al primer tutor justo después de guardar sus materias.
            var previous = SelectedAdminAdvisor?.Id;

            AdminAdvisors.Clear();
            foreach (var advisor in await _api.GetAdminAdvisorsAsync())
            {
                AdminAdvisors.Add(advisor);
            }

            SelectedAdminAdvisor = AdminAdvisors.FirstOrDefault(a => a.Id == previous)
                                   ?? AdminAdvisors.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
    }

    partial void OnSelectedSubjectChanged(SubjectItem? value) => _ = LoadAdvisorsAsync();

    partial void OnSelectedAdvisorChanged(AdvisorItem? value) => _ = LoadSlotsAsync();

    partial void OnSelectedAdminAdvisorChanged(AdvisorItem? value)
    {
        AdminSubjects.Clear();
        if (value is null)
        {
            return;
        }

        var assigned = value.Subjects.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var subject in Subjects)
        {
            AdminSubjects.Add(new AssignableSubject
            {
                Id = subject.Id,
                Name = subject.Name,
                IsAssigned = assigned.Contains(subject.Name)
            });
        }
    }

    [RelayCommand]
    private async Task SaveAssignmentsAsync()
    {
        if (SelectedAdminAdvisor is null)
        {
            Status = "Elige primero un tutor.";
            return;
        }

        try
        {
            var ids = AdminSubjects.Where(s => s.IsAssigned).Select(s => s.Id).ToList();
            await _api.SaveAdvisorSubjectsAsync(SelectedAdminAdvisor.Id, ids);
            Status = $"Materias de {SelectedAdminAdvisor.FullName} actualizadas. El catálogo del alumno usará esta lista.";
            await LoadAdminAsync();
        }
        catch (Exception ex)
        {
            // Quitar una materia con asesorías vivas se rechaza a propósito;
            // el porqué tiene que llegar a la pantalla.
            Status = ex.Message;
        }
    }

    /// <summary>
    /// Qué asesorías toca enseñar según el rol activo: las del alumno, las de
    /// la agenda del asesor o todas, si es dirección.
    /// </summary>
    private Task RefreshForRoleAsync()
    {
        if (Session is null)
        {
            return Task.CompletedTask;
        }

        if (IsAdmin)
        {
            return RefreshSessionsAsync(null, null);
        }

        if (IsAdvisor)
        {
            return RefreshSessionsAsync(null, Session.ProfileId);
        }

        if (IsStudent)
        {
            return RefreshSessionsAsync(Session.ProfileId, null);
        }

        return Task.CompletedTask;
    }

    private async Task RefreshSessionsAsync(int? studentId, int? advisorId)
    {
        Sessions.Clear();
        foreach (var session in await _api.GetSessionsAsync(studentId, advisorId))
        {
            Sessions.Add(session);
        }
    }
}

public partial class AssignableSubject : ObservableObject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isAssigned;
}
