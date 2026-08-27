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

        // Background delivery pipeline (Email/Sms): one queue for the process's lifetime and
        // one background reader — see NotificationDeliveryQueue's own doc comment. Mirrors
        // HMS.Modules.Documents' identical scan pipeline registration exactly.
        services.AddSingleton<INotificationDeliveryQueue, NotificationDeliveryQueue>();

        // Real senders, config-driven under Notifications:Smtp:*/Notifications:Sms:* — both
        // no-op with a logged warning when unconfigured rather than throwing, since Email/Sms
        // are best-effort channels (see each sender's own doc comment).
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddHttpClient<ISmsSender, HttpSmsSender>();

        services.AddHostedService<NotificationDeliveryBackgroundService>();

        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationTemplateService, NotificationTemplateService>();
        services.AddScoped<INotificationPreferenceService, NotificationPreferenceService>();

        // Registered explicitly, not AddValidatorsFromAssemblyContaining — that scanner only
        // finds *public* IValidator<T> implementations, and this module's validators are
        // internal by design (docs/DeveloperHandbook.md §8/§20).
        services.AddScoped<IValidator<NotifyRequest>, NotifyRequestValidator>();
        services.AddScoped<IValidator<CreateNotificationTemplateRequest>, CreateNotificationTemplateRequestValidator>();
        services.AddScoped<IValidator<UpdateNotificationTemplateRequest>, UpdateNotificationTemplateRequestValidator>();
        services.AddScoped<IValidator<UpdateNotificationPreferenceRequest>, UpdateNotificationPreferenceRequestValidator>();

        return services;
    }
}
