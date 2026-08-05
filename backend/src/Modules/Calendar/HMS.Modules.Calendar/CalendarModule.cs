using FluentValidation;
using HMS.Modules.Calendar.Application;
using HMS.Modules.Calendar.Application.Abstractions;
using HMS.Modules.Calendar.Application.Validators;
using HMS.Modules.Calendar.Contracts;
using HMS.Modules.Calendar.Infrastructure;
using HMS.Modules.Calendar.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.Calendar;

/// <summary>
/// Single composition entry point for this module, called once from
/// HMS.Api/Configuration — mirrors HMS.Modules.HR.HRModule. Registrations stay inline
/// in one file (not split into Service/Validator/Repository registration files like
/// HMS.Modules.Masters) since this module has one entity so far; split out if/when
/// future phases make this file as large as Masters'.
/// </summary>
public static class CalendarModule
{
    public static IServiceCollection AddCalendarModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing 'ConnectionStrings:Default' configuration value.");

        services.AddDbContext<CalendarDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", CalendarDbContext.SchemaName);

                // Migration classes live in HMS.Database.Migrations (per
                // docs/DatabaseArchitecture.md), not in this module's own assembly.
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            }));

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IEventService, EventService>();

        // Registered explicitly rather than via AddValidatorsFromAssemblyContaining: that
        // scanner only finds *public* IValidator<T> implementations, and this module's
        // validators are internal by design (docs/DeveloperHandbook.md §8/§20).
        services.AddScoped<IValidator<CreateEventRequest>, CreateEventRequestValidator>();
        services.AddScoped<IValidator<UpdateEventRequest>, UpdateEventRequestValidator>();
        services.AddScoped<IValidator<MonthlyEventQuery>, MonthlyEventQueryValidator>();
        services.AddScoped<IValidator<BulkCreateEventsRequest>, BulkCreateEventsRequestValidator>();

        return services;
    }
}
