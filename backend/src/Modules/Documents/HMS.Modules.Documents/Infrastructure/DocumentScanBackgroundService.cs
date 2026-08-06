using HMS.Modules.Documents.Application.Abstractions;
using HMS.Modules.Documents.Domain;
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
        await foreach (var documentId in _queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                await ScanOneAsync(documentId, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A failure here must not crash the whole background service — it would
                // silently stop scanning every subsequent upload for the rest of the
                // process's lifetime. The document is simply left Pending for now.
                _logger.LogError(ex, "Failed to scan document {DocumentId}; it remains Pending.", documentId);
            }
        }
    }

    private async Task ScanOneAsync(Guid documentId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IDocumentFileStorage>();
        var scanner = scope.ServiceProvider.GetRequiredService<IVirusScanner>();

        var document = await repository.GetByIdAsync(documentId, cancellationToken);
        if (document is null)
        {
            _logger.LogWarning("Document {DocumentId} was queued for scanning but no longer exists.", documentId);
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
            _logger.LogInformation("Document {DocumentId} scanned clean; marked Available.", documentId);
        }
        else
        {
            document.MarkQuarantined();
            _logger.LogWarning("Document {DocumentId} flagged by scan ({Signature}); marked Quarantined.", documentId, result.SignatureName);
        }

        await repository.SaveChangesAsync(cancellationToken);
    }
}
