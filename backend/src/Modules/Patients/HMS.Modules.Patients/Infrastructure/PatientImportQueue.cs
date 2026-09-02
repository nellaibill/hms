using System.Runtime.CompilerServices;
using System.Threading.Channels;
using HMS.Modules.Patients.Application.Abstractions;

namespace HMS.Modules.Patients.Infrastructure;

/// <summary>
/// Bounded in-memory queues backing IPatientImportQueue — see that interface's remarks.
/// Registered as a singleton (one pair of queues for the process's lifetime); the two
/// background services are each the sole reader of their own queue.
/// </summary>
internal class PatientImportQueue : IPatientImportQueue
{
    // Bounded so a burst of uploads can't grow these without limit; a writer waits rather than
    // dropping work when full (BoundedChannelFullMode.Wait is the Channel default). 50 is
    // generous for "batches awaiting processing" — each item here is a whole file, not a row.
    private readonly Channel<PatientImportValidationQueueItem> _validationChannel = Channel.CreateBounded<PatientImportValidationQueueItem>(50);
    private readonly Channel<PatientImportCommitQueueItem> _commitChannel = Channel.CreateBounded<PatientImportCommitQueueItem>(50);

    public ValueTask EnqueueValidationAsync(PatientImportValidationQueueItem item, CancellationToken cancellationToken)
        => _validationChannel.Writer.WriteAsync(item, cancellationToken);

    public ValueTask EnqueueCommitAsync(PatientImportCommitQueueItem item, CancellationToken cancellationToken)
        => _commitChannel.Writer.WriteAsync(item, cancellationToken);

    public async IAsyncEnumerable<PatientImportValidationQueueItem> DequeueValidationAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in _validationChannel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return item;
        }
    }

    public async IAsyncEnumerable<PatientImportCommitQueueItem> DequeueCommitAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in _commitChannel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return item;
        }
    }
}
