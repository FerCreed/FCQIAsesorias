namespace FCQI.Domain.Sessions;

/// <summary>Papel con el que alguien actúa sobre una asesoría concreta.</summary>
public enum SessionActor
{
    Student,
    Advisor,
    Admin
}

/// <summary>
/// Patrón Estado: cada estado de una asesoría sabe a qué estados puede pasar y
/// quién puede llevarla ahí. Antes estas reglas vivían repartidas en
/// condicionales del handler; ahora el ciclo de vida completo se lee aquí:
///
///   Pendiente  → Confirmada (asesor) · Rechazada (asesor) · Cancelada (alumno o asesor)
///   Confirmada → Realizada (asesor, con bitácora) · Cancelada (alumno o asesor)
///   Cancelada, Rechazada y Realizada son finales.
///
/// Dirección puede hacer cualquier transición válida, nunca revivir un estado final.
/// </summary>
public abstract class SessionState
{
    private static readonly SessionActor[] AdvisorSide = [SessionActor.Advisor, SessionActor.Admin];
    private static readonly SessionActor[] Anyone = [SessionActor.Student, SessionActor.Advisor, SessionActor.Admin];

    public abstract byte Id { get; }
    public abstract string Name { get; }

    /// <summary>Destinos posibles y quién puede elegir cada uno.</summary>
    protected abstract IReadOnlyDictionary<byte, SessionActor[]> Transitions { get; }

    public bool IsFinal => Transitions.Count == 0;

    /// <summary>Si la asesoría todavía ocupa un lugar en el bloque de horario.</summary>
    public bool OccupiesSeat => !IsFinal;

    public bool CanMoveTo(byte target) => Transitions.ContainsKey(target);

    public bool Allows(byte target, SessionActor actor)
        => Transitions.TryGetValue(target, out var who) && who.Contains(actor);

    public static SessionState From(byte id) => id switch
    {
        SessionStatusIds.Pending => new Pending(),
        SessionStatusIds.Confirmed => new Confirmed(),
        SessionStatusIds.Cancelled => new Final(id, SessionStatuses.Cancelled),
        SessionStatusIds.Rejected => new Final(id, SessionStatuses.Rejected),
        SessionStatusIds.Completed => new Final(id, SessionStatuses.Completed),
        _ => throw new InvalidOperationException($"Estado desconocido: {id}.")
    };

    private sealed class Pending : SessionState
    {
        public override byte Id => SessionStatusIds.Pending;
        public override string Name => SessionStatuses.Pending;

        protected override IReadOnlyDictionary<byte, SessionActor[]> Transitions { get; } =
            new Dictionary<byte, SessionActor[]>
            {
                [SessionStatusIds.Confirmed] = AdvisorSide,
                [SessionStatusIds.Rejected] = AdvisorSide,
                [SessionStatusIds.Cancelled] = Anyone
            };
    }

    private sealed class Confirmed : SessionState
    {
        public override byte Id => SessionStatusIds.Confirmed;
        public override string Name => SessionStatuses.Confirmed;

        protected override IReadOnlyDictionary<byte, SessionActor[]> Transitions { get; } =
            new Dictionary<byte, SessionActor[]>
            {
                [SessionStatusIds.Completed] = AdvisorSide,
                [SessionStatusIds.Cancelled] = Anyone
            };
    }

    private sealed class Final(byte id, string name) : SessionState
    {
        public override byte Id => id;
        public override string Name => name;

        protected override IReadOnlyDictionary<byte, SessionActor[]> Transitions { get; } =
            new Dictionary<byte, SessionActor[]>();
    }
}
