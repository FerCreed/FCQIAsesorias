namespace FCQI.Domain.Sessions;

// Papel con el que alguien actúa sobre una asesoría concreta.
public enum SessionActor
{
    // El alumno que solicitó la asesoría.
    Student,

    // El asesor dueño del bloque de horario.
    Advisor,

    // Dirección, con autoridad sobre cualquier asesoría.
    Admin
}

// Patrón Estado: cada estado de una asesoría sabe a qué estados puede pasar y
// quién puede llevarla ahí. Antes estas reglas vivían repartidas en
// condicionales del handler; ahora el ciclo de vida completo se lee aquí:
//
//   Pendiente  → Confirmada (asesor) · Rechazada (asesor) · Cancelada (alumno o asesor)
//   Confirmada → Realizada (asesor, con bitácora) · Cancelada (alumno o asesor)
//   Cancelada, Rechazada y Realizada son finales.
//
// Dirección puede hacer cualquier transición válida, nunca revivir un estado final.
public abstract class SessionState
{
    // Conjuntos de actores compartidos por todos los estados; estáticos para no
    // reservar un arreglo nuevo en cada instancia.
    private static readonly SessionActor[] AdvisorSide = [SessionActor.Advisor, SessionActor.Admin];
    private static readonly SessionActor[] Anyone = [SessionActor.Student, SessionActor.Advisor, SessionActor.Admin];

    // Identificador del estado en estados_sesion.
    public abstract byte Id { get; }

    // Nombre del estado tal como lo expone la API.
    public abstract string Name { get; }

    // Destinos posibles y quién puede elegir cada uno.
    protected abstract IReadOnlyDictionary<byte, SessionActor[]> Transitions { get; }

    // Un estado sin transiciones de salida es final.
    public bool IsFinal => Transitions.Count == 0;

    // Si la asesoría todavía ocupa un lugar en el bloque de horario.
    public bool OccupiesSeat => !IsFinal;

    // Si la transición existe, sin importar quién la pida.
    public bool CanMoveTo(byte target) => Transitions.ContainsKey(target);

    // Si la transición existe y el actor tiene permiso para hacerla.
    public bool Allows(byte target, SessionActor actor)
        => Transitions.TryGetValue(target, out var who) && who.Contains(actor);

    // Fábrica: obtiene el objeto de estado a partir del identificador guardado
    // en la base. Lanza ante un identificador desconocido para no aplicar
    // reglas de otro estado por error.
    public static SessionState From(byte id) => id switch
    {
        SessionStatusIds.Pending => new Pending(),
        SessionStatusIds.Confirmed => new Confirmed(),
        SessionStatusIds.Cancelled => new Final(id, SessionStatuses.Cancelled),
        SessionStatusIds.Rejected => new Final(id, SessionStatuses.Rejected),
        SessionStatusIds.Completed => new Final(id, SessionStatuses.Completed),
        _ => throw new InvalidOperationException($"Estado desconocido: {id}.")
    };

    // Estado inicial: solo el asesor decide, cualquiera puede cancelar.
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

    // Asesoría aceptada: el asesor la cierra como realizada o cualquiera la cancela.
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

    // Una sola clase para los tres estados finales: comparten comportamiento
    // (sin transiciones) y solo difieren en identificador y nombre.
    private sealed class Final(byte id, string name) : SessionState
    {
        public override byte Id => id;
        public override string Name => name;

        protected override IReadOnlyDictionary<byte, SessionActor[]> Transitions { get; } =
            new Dictionary<byte, SessionActor[]>();
    }
}
