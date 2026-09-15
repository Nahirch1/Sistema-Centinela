using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SentinelCase.Application.Common.Interfaces;
using SentinelCase.Application.Features.Telemetry.DetectionRules;
using SentinelCase.Infrastructure.Identity;
using SentinelCase.Infrastructure.Identity.Tokens;
using SentinelCase.Infrastructure.Messaging.Outbox;
using SentinelCase.Infrastructure.Observability;
using SentinelCase.Infrastructure.Persistence;
using SentinelCase.Infrastructure.Persistence.Repositories;

namespace SentinelCase.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(
            "DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found.");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.AddScoped<
            ISecurityIncidentRepository,
            SecurityIncidentRepository>();

        services.AddScoped<
            IIncidentHistoryRepository,
            IncidentHistoryRepository>();

        services.AddScoped<
            IIncidentNoteRepository,
            IncidentNoteRepository>();

        services.AddScoped<
            IMonitoredAssetRepository,
            MonitoredAssetRepository>();

        services.AddScoped<
            ISecurityEventRepository,
            SecurityEventRepository>();

        services.AddScoped<
            IIncidentDetectionRule,
            RepeatedFailedLoginDetectionRule>();

        services.AddScoped<JwtTokenService>();

        services.AddSingleton<SentinelCaseMetrics>();

        services.AddSingleton<ISentinelCaseMetrics>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    SentinelCaseMetrics>());

        services.AddHostedService<OutboxProcessor>();

        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
