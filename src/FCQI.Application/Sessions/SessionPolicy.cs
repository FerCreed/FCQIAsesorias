using FCQI.Application.Common;
using FCQI.Application.Interfaces;
using FCQI.Domain.Entities;
using FCQI.Domain.Sessions;

namespace FCQI.Application.Sessions;

/// <summary>
/// Reglas de negocio configurables de las asesorías.
/// <c>ChangeLimitHours</c>: hasta cuántas horas antes de la cita puede el
/// alumno cancelar o reprogramar (BPMN 11). Se lee de <c>Sessions:ChangeLimitHours</c>.
/// </summary>
public record SessionPolicy(int ChangeLimitHours = 24)
{
    public void EnsureStudentCanChange(DateTime scheduledUtc, IClock clock)
    {
        if (scheduledUtc - clock.UtcNow < TimeSpan.FromHours(ChangeLimitHours))
        {
            throw new InvalidOperationException(
                $"Solo puedes cancelar o reprogramar hasta {ChangeLimitHours} horas antes de la cita.");
        }
    }

    /// <summary>
    /// Con qué papel actúa quien llama sobre esta asesoría, o null si no es suya.
    /// El asesor tiene prioridad: un asesor par que esté viendo la pantalla
    /// como alumno sigue siendo el asesor de sus propias asesorías.
    /// </summary>
    public static SessionActor? ActorFor(AdvisorySession session, Caller caller)
    {
        if (caller.IsAdmin) return SessionActor.Admin;
        if (caller.IsAdvisor && caller.PersonId == session.AdvisorId) return SessionActor.Advisor;
        if (caller.IsStudent && caller.PersonId == session.StudentId) return SessionActor.Student;
        return null;
    }

    public static SessionActor RequireActor(AdvisorySession session, Caller caller)
        => ActorFor(session, caller) ?? throw new ForbiddenOperationException("Esta asesoría no es tuya.");
}
