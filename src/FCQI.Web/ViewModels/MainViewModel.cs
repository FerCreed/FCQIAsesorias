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
    private string _returnPage = "home";
    private bool _resumeRequest;

    [ObservableProperty] private string _page = "home";
    [ObservableProperty] private string _status = "Consulta el catálogo 2026-2. Inicia sesión solo para solicitar una asesoría.";
    [ObservableProperty] private string _loginError = string.Empty;
    [ObservableProperty] private AuthResultItem? _session;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _subjectQuery = string.Empty;
    [ObservableProperty] private SubjectItem? _selectedSubject;
    [ObservableProperty] private AdvisorItem? _selectedAdvisor;
    [ObservableProperty] private AvailabilityItem? _selectedSlot;
    [ObservableProperty] private SessionItem? _selectedSession;
    [ObservableProperty] private AdvisorItem? _selectedAdminAdvisor;
    [ObservableProperty] private string _topic = "Dudas del parcial";
    [ObservableProperty] private string _welcome = "Sistema de Asesorías FCQI";
    [ObservableProperty] private bool _googleReady;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _activeRole;

    public ObservableCollection<string> Roles { get; } = [];

    public ObservableCollection<SubjectItem> Subjects { get; } = [];
    public ObservableCollection<SubjectItem> VisibleSubjects { get; } = [];
    public ObservableCollection<AdvisorItem> Advisors { get; } = [];
    public ObservableCollection<AvailabilityItem> Slots { get; } = [];
    public ObservableCollection<SessionItem> Sessions { get; } = [];
    public ObservableCollection<AdvisorItem> AdminAdvisors { get; } = [];
    public ObservableCollection<AssignableSubject> AdminSubjects { get; } = [];

    public bool IsStudent => ActiveRole == UserRoles.Student;
    public bool IsAdvisor => ActiveRole == UserRoles.Advisor;
    public bool IsAdmin => ActiveRole == UserRoles.Admin;
    public bool IsLoggedIn => Session is not null;
    public bool CanSwitchRole => IsLoggedIn && Roles.Count > 1;
    public bool CanDecide => IsAdvisor || IsAdmin;
    public bool IsGuest => Session is null;
    public bool ShowLogin => Page == "login";
    public bool ShowHome => Page == "home";
    public bool ShowSearch => Page == "search";
    public bool ShowSessions => IsLoggedIn && Page == "sessions";
    public bool ShowAdmin => IsAdmin && Page == "admin";
    public bool ShowHero => Page is "home" or "login";
    public string RequestButtonText => IsStudent ? "Enviar solicitud" : "Iniciar sesión para solicitar";
    public bool HasVisibleSubjects => VisibleSubjects.Count > 0;
    public bool HasAdvisors => Advisors.Count > 0;
    public bool HasSlots => Slots.Count > 0;
    public bool HasSessions => Sessions.Count > 0;
    public bool ShowSubjectStep => SelectedSubject is null;
    public bool ShowAdvisorStep => SelectedSubject is not null && SelectedAdvisor is null;
    public bool ShowSlotStep => SelectedAdvisor is not null;
    public bool ShowRequestStep => SelectedSlot is not null;
    public bool ShowCatalogBack => !ShowSubjectStep;
    public bool EmptySubjects => ShowSubjectStep && !HasVisibleSubjects;
    public bool EmptyAdvisors => ShowAdvisorStep && !HasAdvisors;
    public bool EmptySlots => ShowSlotStep && !HasSlots;
    public bool CanCancelSelected => IsStudent && SelectedSession?.Status == "Pendiente";
    public bool HasLoginError => !string.IsNullOrWhiteSpace(LoginError);
    public string CatalogHeading => SelectedAdvisor is not null
        ? "Horarios"
        : SelectedSubject is not null
            ? "Tutores autorizados"
            : "Materias";
    public string CatalogContext => SelectedAdvisor is not null
        ? $"{SelectedSubject?.Name} · {SelectedAdvisor.FullName}"
        : SelectedSubject is not null
            ? SelectedSubject.Name
            : "Busca y elige una materia. Luego verás tutores y horarios en este mismo panel.";
    public string RoleHint => ActiveRole switch
    {
        "Alumno" => "Reserva con tutores autorizados por dirección para el periodo 2026-2.",
        "Asesor" => "Revisa las solicitudes de tus alumnos y confirma o rechaza cada una.",
        "Directivo" => "Asigna las materias que cubre cada tutor del programa.",
        _ => "Consulta materias, tutores y horarios del periodo 2026-2. Inicia sesión solo cuando quieras solicitar una asesoría."
    };

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
            Status = "Catálogo abierto: puedes ver materias y tutores sin iniciar sesión.";
            await LoadCatalogAsync();
        }
        catch (Exception ex)
        {
            Status = $"No se pudo conectar con el servidor. Verifica que la API esté en {FcqiApiClient.BaseUrl}. {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            SetLoginError("Ingresa tu correo institucional @uabc.edu.mx.");
            return;
        }

        if (!Email.Trim().EndsWith("@uabc.edu.mx", StringComparison.OrdinalIgnoreCase))
        {
            SetLoginError("Ese correo no es institucional. Solo se permiten cuentas @uabc.edu.mx.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            SetLoginError("Ingresa tu contraseña.");
            return;
        }

        try
        {
            IsBusy = true;
            LoginError = string.Empty;
            OnPropertyChanged(nameof(HasLoginError));
            Session = await _api.LoginAsync(Email.Trim(), Password);
            BeginSession(Session);
            Password = string.Empty;
            Welcome = $"Hola, {Session.FullName}";
            var next = string.IsNullOrWhiteSpace(_returnPage) ? "home" : _returnPage;
            if (next is "sessions" && !IsStudent && !IsAdvisor)
            {
                next = "home";
            }

            if (next is "admin" && !IsAdmin)
            {
                next = "home";
            }

            Page = next;
            _returnPage = "home";
            NotifyNav();
            Status = $"{Session.Role} · {Session.Email}";
            await LoadHomeDataAsync();
            if (_resumeRequest && IsStudent)
            {
                _resumeRequest = false;
                await RequestSessionAsync();
            }

            _resumeRequest = false;
        }
        catch (Exception ex)
        {
            SetLoginError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void BeginSession(AuthResultItem session)
    {
        _api.UseToken(session.Token);
        Roles.Clear();
        var roles = session.Roles.Count > 0 ? session.Roles : [session.Role];
        foreach (var role in roles)
        {
            Roles.Add(role);
        }

        ActiveRole = roles.Contains(session.Role) ? session.Role : roles[0];
    }

    partial void OnActiveRoleChanged(string? value)
    {
        NotifyNav();
        if (Session is null || string.IsNullOrEmpty(value))
        {
            return;
        }

        Status = Roles.Count > 1
            ? $"Estás usando el sistema como {value}."
            : $"{value} · {Session.Email}";
        _ = LoadHomeDataAsync();
    }

    private void SetLoginError(string message)
    {
        LoginError = message;
        Status = message;
        OnPropertyChanged(nameof(HasLoginError));
    }

    [RelayCommand]
    private void Logout()
    {
        Session = null;
        ActiveRole = null;
        Roles.Clear();
        _api.UseToken(null);
        _resumeRequest = false;
        _returnPage = "home";
        Page = "home";
        Welcome = "Sistema de Asesorías FCQI";
        LoginError = string.Empty;
        OnPropertyChanged(nameof(HasLoginError));
        Status = "Sesión cerrada. El catálogo sigue disponible; inicia sesión solo para solicitar.";
        Sessions.Clear();
        AdminAdvisors.Clear();
        AdminSubjects.Clear();
        SelectedSession = null;
        SelectedAdminAdvisor = null;
        NotifyNav();
        _ = LoadCatalogAsync();
    }

    [RelayCommand]
    private void Go(string page)
    {
        if (page is "sessions" && !IsLoggedIn)
        {
            RequireLogin("sessions", "Inicia sesión para ver tus asesorías.");
            return;
        }

        if (page is "admin" && !IsAdmin)
        {
            if (!IsLoggedIn)
            {
                RequireLogin("admin", "Inicia sesión con una cuenta de dirección.");
                return;
            }

            Status = "Esta sección es solo para dirección del programa.";
            return;
        }

        Page = page;
        NotifyNav();
    }

    [RelayCommand]
    private void BackCatalog()
    {
        if (SelectedAdvisor is not null)
        {
            SelectedAdvisor = null;
            return;
        }

        if (SelectedSubject is not null)
        {
            SelectedSubject = null;
        }
    }

    [RelayCommand]
    private void OpenLogin()
    {
        RequireLogin(Page is "login" ? "home" : Page, "Ingresa con tu correo institucional @uabc.edu.mx.");
    }

    private void RequireLogin(string returnPage, string message)
    {
        _returnPage = returnPage;
        Page = "login";
        Status = message;
        NotifyNav();
    }

    private void NotifyNav()
    {
        OnPropertyChanged(nameof(IsStudent));
        OnPropertyChanged(nameof(IsAdvisor));
        OnPropertyChanged(nameof(IsAdmin));
        OnPropertyChanged(nameof(IsLoggedIn));
        OnPropertyChanged(nameof(CanSwitchRole));
        OnPropertyChanged(nameof(CanDecide));
        OnPropertyChanged(nameof(IsGuest));
        OnPropertyChanged(nameof(ShowLogin));
        OnPropertyChanged(nameof(ShowHome));
        OnPropertyChanged(nameof(ShowSearch));
        OnPropertyChanged(nameof(ShowSessions));
        OnPropertyChanged(nameof(ShowAdmin));
        OnPropertyChanged(nameof(ShowHero));
        OnPropertyChanged(nameof(RequestButtonText));
        OnPropertyChanged(nameof(RoleHint));
        NotifyCatalog();
    }

    private void NotifyCatalog()
    {
        OnPropertyChanged(nameof(HasVisibleSubjects));
        OnPropertyChanged(nameof(HasAdvisors));
        OnPropertyChanged(nameof(HasSlots));
        OnPropertyChanged(nameof(HasSessions));
        OnPropertyChanged(nameof(ShowSubjectStep));
        OnPropertyChanged(nameof(ShowAdvisorStep));
        OnPropertyChanged(nameof(ShowSlotStep));
        OnPropertyChanged(nameof(ShowRequestStep));
        OnPropertyChanged(nameof(ShowCatalogBack));
        OnPropertyChanged(nameof(EmptySubjects));
        OnPropertyChanged(nameof(EmptyAdvisors));
        OnPropertyChanged(nameof(EmptySlots));
        OnPropertyChanged(nameof(CanCancelSelected));
        OnPropertyChanged(nameof(CatalogHeading));
        OnPropertyChanged(nameof(CatalogContext));
    }

    [RelayCommand]
    private async Task LoadHomeDataAsync()
    {
        try
        {
            await LoadCatalogAsync();

            if (IsStudent && Session is not null)
            {
                await RefreshSessionsAsync(Session.ProfileId, null);
            }
            else if (IsAdvisor && Session is not null)
            {
                await RefreshSessionsAsync(null, Session.ProfileId);
            }
            else if (IsAdmin)
            {
                await LoadAdminAsync();
            }
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
    }

    private async Task LoadCatalogAsync()
    {
        Subjects.Clear();
        foreach (var subject in await _api.GetSubjectsAsync())
        {
            Subjects.Add(subject);
        }

        ApplySubjectFilter();
    }

    [RelayCommand]
    private async Task LoadAdvisorsAsync()
    {
        if (SelectedSubject is null)
        {
            Advisors.Clear();
            Slots.Clear();
            NotifyCatalog();
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

            NotifyCatalog();
            Status = Advisors.Count == 0
                ? "Dirección aún no asignó tutores a esta materia."
                : $"{Advisors.Count} tutores asignados por dirección a {SelectedSubject.Name}.";
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
            Slots.Clear();
            NotifyCatalog();
            return;
        }

        try
        {
            Slots.Clear();
            foreach (var slot in await _api.GetAvailabilitiesAsync(SelectedAdvisor.Id))
            {
                Slots.Add(slot);
            }

            NotifyCatalog();
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
    }

    [RelayCommand]
    private async Task RequestSessionAsync()
    {
        if (SelectedSubject is null || SelectedAdvisor is null || SelectedSlot is null)
        {
            Status = "Elige materia, tutor y horario.";
            return;
        }

        if (Session is null)
        {
            _resumeRequest = true;
            RequireLogin("search", "Inicia sesión con @uabc.edu.mx para enviar la solicitud.");
            return;
        }

        if (!IsStudent)
        {
            Status = "Solo una cuenta de alumno puede solicitar asesorías.";
            return;
        }

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
                StudentId = Session.ProfileId,
                AdvisorId = SelectedAdvisor.Id,
                SubjectId = SelectedSubject.Id,
                AvailabilityId = SelectedSlot.Id,
                ScheduledAt = day.Add(start),
                Topic = Topic
            });
            Status = "Solicitud enviada. Quedó pendiente de confirmación del tutor.";
            await RefreshSessionsAsync(Session.ProfileId, null);
            Page = "sessions";
            NotifyNav();
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
    }

    [RelayCommand]
    private async Task ConfirmSessionAsync()
    {
        if (SelectedSession is null)
        {
            return;
        }

        await _api.UpdateStatusAsync(SelectedSession.Id, "Confirmada");
        Status = "Solicitud confirmada.";
        if (Session is not null)
        {
            await RefreshSessionsAsync(null, Session.ProfileId);
        }
    }

    [RelayCommand]
    private async Task CancelSessionAsync()
    {
        if (!CanCancelSelected || SelectedSession is null || Session is null)
        {
            Status = "Solo puedes cancelar solicitudes pendientes.";
            return;
        }

        try
        {
            await _api.UpdateStatusAsync(SelectedSession.Id, "Cancelada");
            Status = "Solicitud cancelada.";
            SelectedSession = null;
            await RefreshSessionsAsync(Session.ProfileId, null);
            NotifyCatalog();
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
    }

    [RelayCommand]
    private async Task RejectSessionAsync()
    {
        if (SelectedSession is null)
        {
            return;
        }

        await _api.UpdateStatusAsync(SelectedSession.Id, "Rechazada");
        Status = "Solicitud rechazada.";
        if (Session is not null)
        {
            await RefreshSessionsAsync(null, Session.ProfileId);
        }
    }

    [RelayCommand]
    private async Task LoadAdminAsync()
    {
        AdminAdvisors.Clear();
        foreach (var advisor in await _api.GetAdminAdvisorsAsync())
        {
            AdminAdvisors.Add(advisor);
        }

        if (SelectedAdminAdvisor is null)
        {
            SelectedAdminAdvisor = AdminAdvisors.FirstOrDefault();
        }
    }

    partial void OnSubjectQueryChanged(string value) => ApplySubjectFilter();

    private void ApplySubjectFilter()
    {
        VisibleSubjects.Clear();
        var query = SubjectQuery?.Trim() ?? string.Empty;
        foreach (var subject in Subjects)
        {
            if (query.Length == 0 ||
                subject.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                subject.Code.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                VisibleSubjects.Add(subject);
            }
        }

        OnPropertyChanged(nameof(HasVisibleSubjects));
    }

    partial void OnSelectedSubjectChanged(SubjectItem? value)
    {
        NotifyCatalog();
        if (SelectedAdvisor is not null)
        {
            SelectedAdvisor = null;
        }

        if (SelectedSlot is not null)
        {
            SelectedSlot = null;
        }

        _ = LoadAdvisorsAsync();
    }

    partial void OnSelectedAdvisorChanged(AdvisorItem? value)
    {
        NotifyCatalog();
        if (SelectedSlot is not null)
        {
            SelectedSlot = null;
        }

        _ = LoadSlotsAsync();
    }

    partial void OnSelectedSlotChanged(AvailabilityItem? value) => NotifyCatalog();

    partial void OnSelectedSessionChanged(SessionItem? value) => NotifyCatalog();

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
            return;
        }

        var ids = AdminSubjects.Where(s => s.IsAssigned).Select(s => s.Id).ToList();
        await _api.SaveAdvisorSubjectsAsync(SelectedAdminAdvisor.Id, ids);
        Status = $"Materias de {SelectedAdminAdvisor.FullName} actualizadas. El catálogo del alumno usará esta lista.";
        await LoadAdminAsync();
    }

    private async Task RefreshSessionsAsync(int? studentId, int? advisorId)
    {
        Sessions.Clear();
        foreach (var session in await _api.GetSessionsAsync(studentId, advisorId))
        {
            Sessions.Add(session);
        }

        OnPropertyChanged(nameof(HasSessions));
    }
}

public partial class AssignableSubject : ObservableObject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isAssigned;
}
