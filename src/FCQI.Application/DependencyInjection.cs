using FCQI.Application.Common;
using FCQI.Application.Sessions.Events;
using Microsoft.Extensions.DependencyInjection;

namespace FCQI.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registra todos los casos de uso (clases que terminan en <c>Handler</c>)
    /// para no mantener a mano una lista en Program.cs que se olvida al crear
    /// uno nuevo.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var handlers = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.Name.EndsWith("Handler"));

        foreach (var handler in handlers)
        {
            services.AddScoped(handler);
        }

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
