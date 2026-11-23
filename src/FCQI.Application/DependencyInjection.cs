using FCQI.Application.Common;
using FCQI.Application.Sessions.Events;
using Microsoft.Extensions.DependencyInjection;

namespace FCQI.Application;

// Punto único de registro de la capa de aplicación en el contenedor de
// dependencias; la API solo llama a AddApplication.
public static class DependencyInjection
{
    // Registra todos los casos de uso (clases que terminan en Handler)
    // para no mantener a mano una lista en Program.cs que se olvida al crear
    // uno nuevo.
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var handlers = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.Name.EndsWith("Handler"));

        // Scoped: cada petición HTTP obtiene su propio handler, ligado al mismo DbContext.
        foreach (var handler in handlers)
        {
            services.AddScoped(handler);
        }

        // Servicios auxiliares que no siguen la convención de nombre *Handler.
        services.AddScoped<CurrentTerm>();
        services.AddScoped<SessionEventPublisher>();

        // El orden importa: primero se anota el autor en el historial y se
        // genera el enlace, después se avisa, para que el correo ya lo lleve.
        services.AddScoped<ISessionObserver, StatusHistoryAuthor>();
        services.AddScoped<ISessionObserver, MeetingLinkObserver>();
        services.AddScoped<ISessionObserver, SessionNotifier>();

        return services;
    }
}
