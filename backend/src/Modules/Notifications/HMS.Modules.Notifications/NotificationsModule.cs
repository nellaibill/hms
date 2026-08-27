using FluentValidation;
using HMS.Modules.Notifications.Application;
using HMS.Modules.Notifications.Application.Abstractions;
using HMS.Modules.Notifications.Application.Validators;
using HMS.Modules.Notifications.Contracts;
using HMS.Modules.Notifications.Infrastructure;
using HMS.Modules.Notifications.Infrastructure.Repositories;
using HMS.Shared.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.Notifications;

/// <summary>
/// Single composition entry point for this module, called once from
/// HMS.Api/Configuration/ModuleRegistration.cs — mirrors every other module's AddXModule.
/// </summary>
public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services, IConfiguration configuration)
    {
        // HMS Multi-Tenancy Phase C: resolved per-request from ITenantContext — see
        // HMS.Modules.Identity.IdentityModule's identical registration for the full
        // rationale (mirrors HMS.Modules.Pharmacy.PharmacyModule's registration exactly).
        services.AddDbContext<NotificationsDbContext>((sp, options) =>
        {
            var tenantContext = sp.GetRequiredService<ITenantContext>();
            if (!tenantContext.IsResolved)
            {
                throw new InvalidOperationException(
                    "NotificationsDbContext was resolved without a tenant having been established for this request.");
            }

            options.UseNpgsql(tenantContext.ConnectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", NotificationsDbContext.SchemaName);
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            });
        });

        services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationRecipientRepository, NotificationRecipientRepository>();
        services.AddScoped<INotificationDeliveryRepository, NotificationDeliveryRepository>();

        services.AddScoped<INotificationService, NotificationService>();

        // Registered explicitly, not AddValidatorsFromAssemblyContaining — that scanner only
        // finds *public* IValidator<T> implementations, and this module's validators are
        // internal by design (docs/DeveloperHandbook.md §8/§20).
        services.AddScoped<IValidator<NotifyRequest>, NotifyRequestValidator>();

        return services;
    }
}
