using FluentValidation;
using HMS.Modules.Documents.Application;
using HMS.Modules.Documents.Application.Abstractions;
using HMS.Modules.Documents.Application.Indexing;
using HMS.Modules.Documents.Application.Security;
using HMS.Modules.Documents.Application.Validators;
using HMS.Modules.Documents.Contracts;
using HMS.Modules.Documents.Infrastructure;
using HMS.Modules.Documents.Infrastructure.Repositories;
using HMS.Modules.Documents.Infrastructure.TextExtraction;
using HMS.Shared.Kernel;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
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
                npgsql.UseVector();
            });
        });

        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentFileStorage, DocumentFileStorage>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IDocumentAccessPolicy, DocumentAccessPolicy>();

        // RAG phase 1: extract -> chunk -> store into documents.document_chunks, run after
        // each clean scan (DocumentScanBackgroundService) and via reindex/backfill.
        services.AddScoped<IDocumentChunkRepository, DocumentChunkRepository>();
        services.AddScoped<IDocumentIndexer, DocumentIndexer>();
        services.AddSingleton<IDocumentTextExtractor, PdfTextExtractor>();
        services.AddSingleton<IDocumentTextExtractor, DocxTextExtractor>();
        services.AddSingleton<IDocumentTextExtractor, XlsxTextExtractor>();

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
