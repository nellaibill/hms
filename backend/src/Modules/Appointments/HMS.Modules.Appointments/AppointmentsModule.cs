using FluentValidation;
using HMS.Modules.Appointments.Application;
using HMS.Modules.Appointments.Application.Abstractions;
using HMS.Modules.Appointments.Application.Validators;
using HMS.Modules.Appointments.Contracts;
using HMS.Modules.Appointments.Infrastructure;
using HMS.Modules.Appointments.Infrastructure.Repositories;
using HMS.Shared.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HMS.Modules.Appointments;

/// <summary>
/// Single composition entry point for this module, called once from HMS.Api/Configuration —
/// mirrors HMS.Modules.Calendar.CalendarModule.
/// </summary>
public static class AppointmentsModule
{
    public static IServiceCollection AddAppointmentsModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Resolved per request from ITenantContext — see HMS.Modules.Identity.IdentityModule's
        // identical registration for the full rationale.
        services.AddDbContext<AppointmentsDbContext>((sp, options) =>
        {
            var tenantContext = sp.GetRequiredService<ITenantContext>();
            if (!tenantContext.IsResolved)
            {
                throw new InvalidOperationException(
                    "AppointmentsDbContext was resolved without a tenant having been established for this request.");
            }

            options.UseNpgsql(tenantContext.ConnectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", AppointmentsDbContext.SchemaName);

                // Migration classes live in HMS.Database.Migrations (per
                // docs/DatabaseArchitecture.md), not in this module's own assembly.
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            });
        });

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<AppointmentClock>();

        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IConsultantBlockRepository, ConsultantBlockRepository>();
        services.AddScoped<IAppointmentNumberGenerator, AppointmentNumberGenerator>();
        services.AddScoped<IAppointmentService, AppointmentService>();

        // Registered explicitly rather than via AddValidatorsFromAssemblyContaining: that scanner
        // only finds *public* validators, and these are internal by design
        // (docs/DeveloperHandbook.md §8/§20).
        services.AddScoped<IValidator<BookAppointmentRequest>, BookAppointmentRequestValidator>();
        services.AddScoped<IValidator<RescheduleAppointmentRequest>, RescheduleAppointmentRequestValidator>();
        services.AddScoped<IValidator<CancelAppointmentRequest>, CancelAppointmentRequestValidator>();
        services.AddScoped<IValidator<MarkAppointmentArrivedRequest>, MarkAppointmentArrivedRequestValidator>();
        services.AddScoped<IValidator<AppointmentListQuery>, AppointmentListQueryValidator>();
        services.AddScoped<IValidator<AppointmentSlotsQuery>, AppointmentSlotsQueryValidator>();
        services.AddScoped<IValidator<AppointmentSummaryQuery>, AppointmentSummaryQueryValidator>();
        services.AddScoped<IValidator<CreateConsultantBlockRequest>, CreateConsultantBlockRequestValidator>();

        return services;
    }
}
