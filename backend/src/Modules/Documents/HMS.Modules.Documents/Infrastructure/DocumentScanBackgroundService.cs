using HMS.Modules.Documents.Application.Abstractions;
using HMS.Modules.Documents.Domain;
using HMS.Shared.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Documents.Infrastructure;

/// <summary>
/// Drains IDocumentScanQueue and moves each document from Pending to Available/Quarantined
/// (US-9). Registered as a hosted service (singleton), so it resolves the scoped repository/
/// file storage/scanner through a fresh DI scope per item — the same pattern ASP.NET Core's
/// own background-task samples use for a singleton service that needs scoped dependencies.
/// </summary>
internal class DocumentScanBackgroundService : BackgroundService
{
    private readonly IDocumentScanQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DocumentScanBackgroundService> _logger;

    public DocumentScanBackgroundService(IDocumentScanQueue queue, IServiceScopeFactory scopeFactory, ILogger<DocumentScanBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in _queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                await ScanOneAsync(item, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A failure here must not crash the whole background service — it would
                // silently stop scanning every subsequent upload for the rest of the
                // process's lifetime. The document is simply left Pending for now.
                _logger.LogError(ex, "Failed to scan document {DocumentId}; it remains Pending.", item.DocumentId);
            }
        }
    }

    private async Task ScanOneAsync(ScanQueueItem item, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        // Must happen before anything resolves a tenant-aware DbContext (IDocumentRepository
        // below) — this scope has no HTTP request of its own for TenantResolutionMiddleware to
        // have populated ITenantContext from, so without this every resolution would throw
        // "resolved without a tenant having been established," permanently stranding the
        // document at Pending (see ScanQueueItem's own doc comment).
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        tenantContext.SetTenant(item.TenantId, item.ConnectionString);

        var repository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IDocumentFileStorage>();
        var scanner = scope.ServiceProvider.GetRequiredService<IVirusScanner>();

        var document = await repository.GetByIdAsync(item.DocumentId, cancellationToken);
        if (document is null)
        {
            _logger.LogWarning("Document {DocumentId} was queued for scanning but no longer exists.", item.DocumentId);
            return;
        }

        ScanResult result;
        await using (var content = await fileStorage.OpenReadAsync(document.StorageKey, cancellationToken))
        {
            result = await scanner.ScanAsync(content, cancellationToken);
        }

        if (result.Outcome == ScanOutcome.Clean)
        {
            document.MarkAvailable();
            _logger.LogInformation("Document {DocumentId} scanned clean; marked Available.", item.DocumentId);
        }
        else
        {
            document.MarkQuarantined();
            _logger.LogWarning("Document {DocumentId} flagged by scan ({Signature}); marked Quarantined.", item.DocumentId, result.SignatureName);
        }

        await repository.SaveChangesAsync(cancellationToken);
    }
}
