using FCQI.Application.Common;
using FCQI.Application.Interfaces;
using FCQI.Application.Sessions;
using FCQI.Domain;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Reports;

/// <summary>Reporte individual de una asesoría: lo descargan sus participantes y dirección.</summary>
public class SessionReportQueryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IClock _clock;

    public SessionReportQueryHandler(IApplicationDbContext context, IClock clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<SessionReport> HandleAsync(long sessionId, Caller caller, CancellationToken cancellationToken = default)
    {
        var session = await _context.AdvisorySessions.AsNoTracking()
                          .SingleOrDefaultAsync(s => s.Id == sessionId, cancellationToken)
                      ?? throw new NotFoundException("La asesoría no existe.");
        SessionPolicy.RequireActor(session, caller);

        var record = (await _context.AdvisorySessions.Where(s => s.Id == sessionId).ToRecordsAsync(_clock, cancellationToken)).Single();

        var history = await _context.SessionStatusChanges.AsNoTracking()
            .Where(h => h.SessionId == sessionId)
            .OrderBy(h => h.ChangedAt).ThenBy(h => h.Id)
            .Select(h => new
            {
                h.ChangedAt,
                From = h.FromStatus != null ? h.FromStatus.Name : null,
                To = h.ToStatus.Name,
                By = _context.People.Where(p => p.Id == h.ChangedBy).Select(p => p.DisplayName).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return new SessionReport(
            record,
            history.Select(h => new StatusChangeRecord(_clock.ToLocal(h.ChangedAt), h.From, h.To, h.By)).ToList(),
            _clock.LocalNow);
    }
}

/// <summary>
/// Resumen de un asesor: sus asesorías del periodo (por fecha de la cita) y sus
/// horas trabajadas. El asesor solo puede pedir el suyo; dirección, el de cualquiera.
/// </summary>
public class AdvisorSummaryQueryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IClock _clock;

    public AdvisorSummaryQueryHandler(IApplicationDbContext context, IClock clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<AdvisorSummary> HandleAsync(
        int advisorId, DateOnly from, DateOnly to, Caller caller, CancellationToken cancellationToken = default)
    {
        if (!caller.IsAdmin && !(caller.IsAdvisor && caller.PersonId == advisorId))
        {
            throw new ForbiddenOperationException("Solo puedes descargar tu propio resumen de asesorías.");
        }

        var period = ReportRules.Period(from, to);
        var advisor = await _context.AdvisorProfiles.AsNoTracking()
                          .Where(a => a.PersonId == advisorId)
                          .Select(a => new
                          {
                              a.Person.DisplayName,
                              a.Person.Email,
                              IsStudent = a.Person.StudentProfile != null && a.Person.StudentProfile.IsActive
                          })
                          .SingleOrDefaultAsync(cancellationToken)
                      ?? throw new NotFoundException("El asesor no existe.");

        var (fromUtc, toUtc) = period.ToUtcRange(_clock);
        var rows = await _context.AdvisorySessions
            .Where(s => s.AdvisorId == advisorId && s.ScheduledAt >= fromUtc && s.ScheduledAt < toUtc)
            .ToRecordsAsync(_clock, cancellationToken);

        return new AdvisorSummary(
            advisor.DisplayName, advisor.Email, advisor.IsStudent ? AdvisorKinds.Student : AdvisorKinds.Professor,
            period, ReportMetrics.From(rows), rows, _clock.LocalNow);
    }
}

/// <summary>
/// Todas las asesorías creadas en el sistema desde una fecha (BPMN 6), con sus
/// métricas. Solo dirección.
/// </summary>
public class SessionsExportQueryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IClock _clock;

    public SessionsExportQueryHandler(IApplicationDbContext context, IClock clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<SessionsExport> HandleAsync(ReportFilter filter, Caller caller, CancellationToken cancellationToken = default)
    {
        if (!caller.IsAdmin)
        {
            throw new ForbiddenOperationException("El reporte de todas las asesorías es solo para dirección.");
        }

        var period = ReportRules.Period(filter.From, filter.To ?? DateOnly.FromDateTime(_clock.LocalNow));
        var (fromUtc, toUtc) = period.ToUtcRange(_clock);

        var query = _context.AdvisorySessions.Where(s => s.CreatedAt >= fromUtc && s.CreatedAt < toUtc);
        if (filter.AdvisorId is not null) query = query.Where(s => s.AdvisorId == filter.AdvisorId);
        if (filter.SubjectId is not null) query = query.Where(s => s.SubjectId == filter.SubjectId);
        if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(s => s.Status.Name == filter.Status);

        var rows = await query.ToRecordsAsync(_clock, cancellationToken);
        return new SessionsExport(period, ReportMetrics.From(rows), rows, _clock.LocalNow);
    }
}

internal static class ReportRules
{
    /// <summary>Un año basta para cualquier reporte de ciclo y evita consultas sin límite.</summary>
    public const int MaxDays = 400;

    public static ReportPeriod Period(DateOnly from, DateOnly to)
    {
        if (from == default || to == default)
        {
            throw new BusinessRuleException("Indica la fecha inicial y la final del reporte.");
        }

        if (to < from)
        {
            throw new BusinessRuleException("La fecha final debe ser igual o posterior a la inicial.");
        }

        if (to.DayNumber - from.DayNumber > MaxDays)
        {
            throw new BusinessRuleException($"El periodo no puede pasar de {MaxDays} días.");
        }

        return new ReportPeriod(from, to);
    }
}
