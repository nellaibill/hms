using FluentValidation;
using HMS.Modules.Documents.Application.Abstractions;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Application.Abstractions;
using HMS.Modules.Patients.Application.Validators;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Infrastructure;
using HMS.Modules.Patients.Infrastructure.Repositories;
using HMS.Shared.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.Patients;

/// <summary>
/// Single composition entry point for this module, called once from HMS.Api/Configuration.
/// </summary>
public static class PatientsModule
{
    public static IServiceCollection AddPatientsModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Resolved per-request from ITenantContext — see HMS.Modules.Identity.IdentityModule's
        // identical registration for the full rationale.
        services.AddDbContext<PatientsDbContext>((sp, options) =>
        {
            var tenantContext = sp.GetRequiredService<ITenantContext>();
            if (!tenantContext.IsResolved)
            {
                throw new InvalidOperationException(
                    "PatientsDbContext was resolved without a tenant having been established for this request.");
            }

            options.UseNpgsql(tenantContext.ConnectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", PatientsDbContext.SchemaName);
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            });
        });

        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IPatientIdentifierGenerator, PatientIdentifierGenerator>();
        services.AddScoped<IPatientService, PatientService>();

        services.AddScoped<IPatientVisitRepository, PatientVisitRepository>();
        services.AddScoped<IPatientVisitService, PatientVisitService>();

        // Bulk Excel import (Super Admin only — see PermissionSeedData's
        // "patient-management.import" entry). One in-memory queue/hosted-service pair each for
        // the validate and commit passes, mirroring Documents' scan pipeline — see
        // Infrastructure/PatientImportQueue.cs's remarks.
        services.AddScoped<IPatientImportRepository, PatientImportRepository>();
        services.AddScoped<IPatientImportService, PatientImportService>();
        services.AddSingleton<IPatientImportQueue, PatientImportQueue>();
        services.AddHostedService<PatientImportValidationBackgroundService>();
        services.AddHostedService<PatientImportCommitBackgroundService>();

        // Lets HMS.Modules.Documents validate a Patient owner id exists before accepting an
        // upload against it.
        services.AddScoped<IDocumentOwnerExistenceChecker, PatientDocumentOwnerExistenceChecker>();

        // Registered explicitly rather than via AddValidatorsFromAssemblyContaining: that
        // scanner only finds *public* IValidator<T> implementations, and this module's
        // validators are internal by design.
        services.AddScoped<IValidator<CreatePatientRequest>, CreatePatientRequestValidator>();
        services.AddScoped<IValidator<UpdatePatientRequest>, UpdatePatientRequestValidator>();
        services.AddScoped<IValidator<AddAllergyRequest>, AddAllergyRequestValidator>();
        services.AddScoped<IValidator<AddEmergencyContactRequest>, AddEmergencyContactRequestValidator>();
        services.AddScoped<IValidator<CreatePatientVisitRequest>, CreatePatientVisitRequestValidator>();

        return services;
    }
}
