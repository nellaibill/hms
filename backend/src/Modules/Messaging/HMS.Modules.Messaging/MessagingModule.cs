using FluentValidation;
using HMS.Modules.Messaging.Application;
using HMS.Modules.Messaging.Application.Abstractions;
using HMS.Modules.Messaging.Application.Validators;
using HMS.Modules.Messaging.Contracts;
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

        services.AddScoped<IConversationService, ConversationService>();

        // Registered explicitly, not AddValidatorsFromAssemblyContaining — that scanner only
        // finds *public* IValidator<T> implementations, and this module's validators are
        // internal by design (docs/DeveloperHandbook.md §8/§20).
        services.AddScoped<IValidator<CreateConversationRequest>, CreateConversationRequestValidator>();
        services.AddScoped<IValidator<SendMessageRequest>, SendMessageRequestValidator>();

        return services;
    }
}
