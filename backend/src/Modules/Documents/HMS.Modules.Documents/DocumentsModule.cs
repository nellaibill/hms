using FluentValidation;
using HMS.Modules.Documents.Application;
using HMS.Modules.Documents.Application.Abstractions;
using HMS.Modules.Documents.Application.Security;
using HMS.Modules.Documents.Application.Validators;
using HMS.Modules.Documents.Contracts;
using HMS.Modules.Documents.Infrastructure;
using HMS.Modules.Documents.Infrastructure.Repositories;
using HMS.Shared.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.Documents;

/// <summary>
/// Single composition entry point for this module, called once from
/// HMS.Api/Configuration — mirrors HMS.Modules.Patients.PatientsModule.
/// </summary>
public static class DocumentsModule
{
    public static IServiceCollection AddDocumentsModule(this IServiceCollection services, IConfiguration configuration)
    {
        // HMS Multi-Tenancy Phase C: resolved per-request from ITenantContext — see
        // HMS.Modules.Identity.IdentityModule's identical registration for the full
        // rationale.
        services.AddDbContext<DocumentsDbContext>((sp, options) =>
        {
            var tenantContext = sp.GetRequiredService<ITenantContext>();
            if (!tenantContext.IsResolved)
            {
                throw new InvalidOperationException(
                    "DocumentsDbContext was resolved without a tenant having been established for this request.");
            }

            options.UseNpgsql(tenantContext.ConnectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", DocumentsDbContext.SchemaName);
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            });
        });

        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentFileStorage, DocumentFileStorage>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IDocumentAccessPolicy, DocumentAccessPolicy>();

        // Scan pipeline (US-9): one queue for the process's lifetime, one background reader,
        // and the stub scan engine — see NullVirusScanner's remarks.
        services.AddSingleton<IDocumentScanQueue, DocumentScanQueue>();
        services.AddScoped<IVirusScanner, NullVirusScanner>();
        services.AddHostedService<DocumentScanBackgroundService>();

        // Registered explicitly rather than via AddValidatorsFromAssemblyContaining: that
        // scanner only finds *public* IValidator<T> implementations, and this module's
        // validators are internal by design (mirrors PatientsModule).
        services.AddScoped<IValidator<UploadDocumentRequest>, UploadDocumentRequestValidator>();

        return services;
    }
}
