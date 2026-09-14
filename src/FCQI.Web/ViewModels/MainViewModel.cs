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
    public ObservableCollection<SubjectItem> Subjects { get; } = [];
    public ObservableCollection<AdvisorItem> Advisors { get; } = [];
    public ObservableCollection<AvailabilityItem> Slots { get; } = [];
    public ObservableCollection<SessionItem> Sessions { get; } = [];
    public ObservableCollection<AdvisorItem> AdminAdvisors { get; } = [];
    public ObservableCollection<AssignableSubject> AdminSubjects { get; } = [];

    public bool IsStudent => Session?.Role == "Alumno";
    public bool IsAdvisor => Session?.Role == "Asesor";
    public bool IsAdmin => Session?.Role == "Directivo";
    public bool IsLoggedIn => Session is not null;
    public bool ShowLogin => !IsLoggedIn;
    public bool ShowHome => IsLoggedIn && Page == "home";
    public bool ShowSearch => IsStudent && Page == "search";
    public bool ShowSessions => IsLoggedIn && Page == "sessions";
    public bool ShowAdmin => IsAdmin && Page == "admin";

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
            var profiles = await _api.GetDemoProfilesAsync();
            Profiles.Clear();
            foreach (var profile in profiles)
            {
                Profiles.Add(profile);
            }

            SelectedProfile = Profiles.FirstOrDefault(p => p.Role == "Alumno");
            Status = GoogleReady
                ? "Google OAuth listo: solo correos @uabc.edu.mx."
                : "Demo: entra con un perfil. Para OAuth, configura Authentication:Google:ClientId.";
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
            Welcome = $"¡Bienvenido! {Session.FullName}";
            Page = "home";
            NotifyNav();
            Status = $"Sesión {Session.Role} · {Session.Email}";
            await LoadHomeDataAsync();
        }
        catch (Exception ex)
        {
            Status = $"Login: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Logout()
    {
        Session = null;
        Page = "login";
        Welcome = "Sistema de Asesorías FCQI";
        NotifyNav();
    }

    [RelayCommand]
    private void Go(string page)
    {
        Page = page;
        NotifyNav();
    }

    private void NotifyNav()
    {
        OnPropertyChanged(nameof(IsStudent));
        OnPropertyChanged(nameof(IsAdvisor));
        OnPropertyChanged(nameof(IsAdmin));
        OnPropertyChanged(nameof(IsLoggedIn));
        OnPropertyChanged(nameof(ShowLogin));
        OnPropertyChanged(nameof(ShowHome));
        OnPropertyChanged(nameof(ShowSearch));
        OnPropertyChanged(nameof(ShowSessions));
        OnPropertyChanged(nameof(ShowAdmin));
    }

    [RelayCommand]
    private async Task LoadHomeDataAsync()
    {
        try
        {
            Subjects.Clear();
            foreach (var subject in await _api.GetSubjectsAsync())
            {
                Subjects.Add(subject);
            }

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
            Status = "Solicitud enviada (Pendiente). El tutor la verá en su bandeja.";
            await RefreshSessionsAsync(Session.ProfileId, null);
            Page = "sessions";
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
    }
}

public partial class AssignableSubject : ObservableObject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isAssigned;
}
