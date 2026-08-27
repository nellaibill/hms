using System.Runtime.CompilerServices;
using System.Threading.Channels;
using HMS.Modules.Documents.Application.Abstractions;

namespace HMS.Modules.Documents.Infrastructure;

/// <summary>
/// Bounded in-memory queue backing IDocumentScanQueue — see that interface's remarks for why
/// this is the platform's first background-job mechanism and why an in-memory queue is an
/// acceptable MVP trade for this specific pipeline. Registered as a singleton (one queue for
/// the process's lifetime); DocumentScanBackgroundService is the sole reader.
/// </summary>
internal class DocumentScanQueue : IDocumentScanQueue
{
    // Bounded so a burst of uploads can't grow this without limit; a writer waits rather than
    // dropping work when full (BoundedChannelFullMode.Wait is the Channel default).
    private readonly Channel<ScanQueueItem> _channel = Channel.CreateBounded<ScanQueueItem>(500);

    public ValueTask EnqueueAsync(ScanQueueItem item, CancellationToken cancellationToken)
        => _channel.Writer.WriteAsync(item, cancellationToken);

    public async IAsyncEnumerable<ScanQueueItem> DequeueAllAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return item;
        }
    }
}
