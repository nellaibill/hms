using FluentValidation;
using HMS.Modules.Documents.Application.Abstractions;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Application.Abstractions;
using HMS.Modules.Patients.Application.Validators;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Infrastructure;
using HMS.Modules.Patients.Infrastructure.Repositories;
using HMS.Modules.Patients.Infrastructure.Sync;
using HMS.Shared.Kernel;
using HMS.Shared.Kernel.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

            // Offline-first sync capture (ADR-133): an IOutboxWriter is registered only on an
            // Edge server (Sync:Role = Edge), so everywhere else this adds nothing.
            var outboxWriter = sp.GetService<IOutboxWriter>();
            if (outboxWriter is not null)
            {
                options.AddInterceptors(new PatientOutboxInterceptor(outboxWriter, sp.GetRequiredService<ILogger<PatientOutboxInterceptor>>()));
            }
        });

        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IPatientIdentifierGenerator, PatientIdentifierGenerator>();
        services.AddScoped<IPatientService, PatientService>();

        // HMS.Modules.Sync's seam into patient data (ADR-133): applying snapshots on the cloud,
        // version stamps, backfill and the Cloud Sync page's name lookups.
        services.AddScoped<IPatientSyncService, PatientSyncService>();

        services.AddScoped<IPatientVisitRepository, PatientVisitRepository>();
        services.AddScoped<IPatientVisitService, PatientVisitService>();
        services.AddScoped<IOpdQueryService, OpdQueryService>();

        // Nightly 3 AM IST auto-complete of OP lines left open from an earlier day (ADR-110).
        services.AddScoped<IOpdDayCloseService, OpdDayCloseService>();
        services.AddHostedService<OpdDayCloseSchedulerService>();

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
