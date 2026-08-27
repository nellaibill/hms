using HMS.Modules.Messaging.Application.Abstractions;
using HMS.Modules.Messaging.Infrastructure;
using HMS.Modules.Messaging.Infrastructure.Repositories;
using HMS.Shared.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.Messaging;

/// <summary>
/// Single composition entry point for this module, called once from
/// HMS.Api/Configuration/ModuleRegistration.cs — mirrors every other module's AddXModule.
/// Phase 1 scope only: DbContext + repositories. Application services, validators, and
/// Endpoints controllers are registered here once they exist, in a later phase.
/// </summary>
public static class MessagingModule
{
    public static IServiceCollection AddMessagingModule(this IServiceCollection services, IConfiguration configuration)
    {
        // HMS Multi-Tenancy Phase C: resolved per-request from ITenantContext — see
        // HMS.Modules.Identity.IdentityModule's identical registration for the full
        // rationale (mirrors HMS.Modules.Notifications.NotificationsModule's registration
        // exactly).
        services.AddDbContext<MessagingDbContext>((sp, options) =>
        {
            var tenantContext = sp.GetRequiredService<ITenantContext>();
            if (!tenantContext.IsResolved)
            {
                throw new InvalidOperationException(
                    "MessagingDbContext was resolved without a tenant having been established for this request.");
            }

            options.UseNpgsql(tenantContext.ConnectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", MessagingDbContext.SchemaName);
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            });
        });

        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IConversationParticipantRepository, ConversationParticipantRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();

        return services;
    }
}
