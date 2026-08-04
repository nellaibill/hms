using FluentValidation;
using HMS.Modules.HR.Application;
using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Application.Validators;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Infrastructure;
using HMS.Modules.HR.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.HR;

/// <summary>
/// Single composition entry point for this module, called once from
/// HMS.Api/Configuration — mirrors HMS.Modules.Identity.IdentityModule. Registrations stay
/// inline in one file (not split into Service/Validator/Repository registration files like
/// HMS.Modules.Masters) since this module has a handful of entities so far; split out if/
/// when future phases make this file as large as Masters'.
/// </summary>
public static class HRModule
{
    public static IServiceCollection AddHRModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing 'ConnectionStrings:Default' configuration value.");

        services.AddDbContext<HRDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", HRDbContext.SchemaName);

                // Migration classes live in HMS.Database.Migrations (per
                // docs/DatabaseArchitecture.md), not in this module's own assembly.
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            }));

        services.AddScoped<IShiftRepository, ShiftRepository>();
        services.AddScoped<IShiftService, ShiftService>();

        services.AddScoped<IShiftAssignmentRepository, ShiftAssignmentRepository>();
        services.AddScoped<IShiftAssignmentService, ShiftAssignmentService>();

        services.AddScoped<IWeeklyRosterRepository, WeeklyRosterRepository>();
        services.AddScoped<IWeeklyRosterService, WeeklyRosterService>();

        services.AddScoped<IStaffAvailabilityRepository, StaffAvailabilityRepository>();
        services.AddScoped<IStaffAvailabilityService, StaffAvailabilityService>();

        services.AddScoped<IShiftSwapRequestRepository, ShiftSwapRequestRepository>();
        services.AddScoped<IShiftSwapRequestService, ShiftSwapRequestService>();

        // Registered explicitly rather than via AddValidatorsFromAssemblyContaining: that
        // scanner only finds *public* IValidator<T> implementations, and this module's
        // validators are internal by design (docs/DeveloperHandbook.md §8/§20).
        services.AddScoped<IValidator<CreateShiftRequest>, CreateShiftRequestValidator>();
        services.AddScoped<IValidator<UpdateShiftRequest>, UpdateShiftRequestValidator>();

        services.AddScoped<IValidator<CreateShiftAssignmentRequest>, CreateShiftAssignmentRequestValidator>();
        services.AddScoped<IValidator<UpdateShiftAssignmentRequest>, UpdateShiftAssignmentRequestValidator>();

        services.AddScoped<IValidator<CreateWeeklyRosterRequest>, CreateWeeklyRosterRequestValidator>();
        services.AddScoped<IValidator<UpdateWeeklyRosterRequest>, UpdateWeeklyRosterRequestValidator>();
        services.AddScoped<IValidator<CopyWeeklyRosterRequest>, CopyWeeklyRosterRequestValidator>();
        services.AddScoped<IValidator<MonthlyWeeklyRosterQuery>, MonthlyWeeklyRosterQueryValidator>();

        services.AddScoped<IValidator<CreateStaffAvailabilityRequest>, CreateStaffAvailabilityRequestValidator>();
        services.AddScoped<IValidator<UpdateStaffAvailabilityRequest>, UpdateStaffAvailabilityRequestValidator>();

        services.AddScoped<IValidator<CreateSwapRequest>, CreateSwapRequestValidator>();
        services.AddScoped<IValidator<UpdateSwapRequest>, UpdateSwapRequestValidator>();

        return services;
    }
}
