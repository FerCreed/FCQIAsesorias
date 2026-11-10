using FCQI.Application.Auth;
using FCQI.Application.Common;
using FCQI.Application.Interfaces;
using FCQI.Application.Sessions;
using FCQI.Infrastructure.Auth;
using FCQI.Infrastructure.Messaging;
using FCQI.Infrastructure.Reports;
using FCQI.Infrastructure.Persistence;
using FCQI.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FCQI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
        services.AddSingleton<JwtTokenFactory>();
        services.AddSingleton<IClock, CampusClock>();
        services.AddSingleton(new SessionPolicy(configuration.GetValue("Sessions:ChangeLimitHours", 24)));

        // Adaptadores de servicios externos: SMTP solo si está configurado.
        if (string.IsNullOrWhiteSpace(configuration["Smtp:Host"]))
            services.AddSingleton<IEmailSender, LogEmailSender>();
        else
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<IMeetingLinkProvider, LocationMeetingLinkProvider>();
        services.AddSingleton<IReportRenderer, QuestPdfReportRenderer>();

        return services;
    }
}
