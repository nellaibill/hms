using FluentValidation;
using HMS.Modules.Documents.Application.Abstractions;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Application.Abstractions;
using HMS.Modules.Patients.Application.Validators;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Infrastructure;
using HMS.Modules.Patients.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.Patients;

/// <summary>
/// Single composition entry point for this module, called once from
/// HMS.Api/Configuration — mirrors HMS.Modules.Identity.IdentityModule.
/// </summary>
public static class PatientsModule
{
    public static IServiceCollection AddPatientsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing 'ConnectionStrings:Default' configuration value.");

        services.AddDbContext<PatientsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", PatientsDbContext.SchemaName);

                // Migration classes live in HMS.Database.Migrations (per
                // docs/DatabaseArchitecture.md), not in this module's own assembly.
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            }));

        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IPatientIdentifierGenerator, PatientIdentifierGenerator>();
        services.AddScoped<IPatientFileStorage, PatientFileStorage>();
        services.AddScoped<IPatientService, PatientService>();

        // Lets HMS.Modules.Documents validate a Patient owner id exists before accepting an
        // upload against it (US-1) — see PatientDocumentOwnerExistenceChecker's remarks.
        services.AddScoped<IDocumentOwnerExistenceChecker, PatientDocumentOwnerExistenceChecker>();

        // Registered explicitly rather than via AddValidatorsFromAssemblyContaining: that
        // scanner only finds *public* IValidator<T> implementations, and this module's
        // validators are internal by design (docs/DeveloperHandbook.md §8/§20).
        services.AddScoped<IValidator<CreatePatientRequest>, CreatePatientRequestValidator>();
        services.AddScoped<IValidator<UpdatePatientRequest>, UpdatePatientRequestValidator>();

        return services;
    }
}
