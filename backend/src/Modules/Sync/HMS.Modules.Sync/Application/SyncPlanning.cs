using HMS.Modules.Sync.Contracts;

namespace HMS.Modules.Sync.Application;

/// <summary>
/// The outbox's retry schedule (ADR-133): after the Nth transient failure an event waits
/// <c>schedule[N-1]</c> minutes (the last entry repeats), with ±10% jitter so many patients
/// failing together do not retry in lock-step. After <c>MaxRetryCount</c> retries the event
/// needs attention. Hangfire's own retries are switched off for every sync job, so this is the
/// only retry mechanism and a send is never duplicated by a job retry.
/// </summary>
internal sealed class SyncRetryPolicy
{
    private readonly IReadOnlyList<int> _scheduleMinutes;
    private readonly int _maxRetryCount;
    private readonly Func<double> _random;

    public SyncRetryPolicy(IReadOnlyList<int> scheduleMinutes, int maxRetryCount, Func<double>? random = null)
    {
        if (scheduleMinutes.Count == 0)
        {
            throw new ArgumentException("The retry schedule needs at least one entry.", nameof(scheduleMinutes));
        }

        _scheduleMinutes = scheduleMinutes;
        _maxRetryCount = maxRetryCount;
        _random = random ?? Random.Shared.NextDouble;
    }

    public static SyncRetryPolicy From(SyncOptions options) => new(options.EffectiveRetrySchedule, options.MaxRetryCount);

    /// <param name="retryCountSoFar">The event's retry_count before this failure.</param>
    public RetryDecision OnTransientFailure(int retryCountSoFar)
    {
        var retryCount = retryCountSoFar + 1;
        if (retryCount > _maxRetryCount)
        {
            return new RetryDecision(true, retryCount, null);
        }

        var minutes = _scheduleMinutes[Math.Min(retryCount, _scheduleMinutes.Count) - 1];
        var jitterFactor = 0.9 + (0.2 * _random());
        return new RetryDecision(false, retryCount, TimeSpan.FromMinutes(minutes * jitterFactor));
    }
}

internal readonly record struct RetryDecision(bool Exhausted, int RetryCount, TimeSpan? Delay);

/// <summary>One unsynced event as loaded for sending.</summary>
internal sealed record PendingEvent(
    Guid Id,
    Guid EventId,
    Guid StreamId,
    string AggregateType,
    Guid AggregateId,
    string OperationType,
    long SequenceNumber,
    string Payload,
    int PayloadSchemaVersion,
    string PayloadHash,
    DateTime CreatedAt,
    int RetryCount,
    string? Resolution);

/// <summary>How the whole HTTP call ended, before looking at per-event results.</summary>
internal enum CloudTransportOutcome
{
    /// <summary>200 with a readable body: per-event results apply.</summary>
    Success,

    /// <summary>No answer: DNS, refused connection, socket error or timeout. Nothing is
    /// counted against the events; the probe decides when to try again.</summary>
    Connectivity,

    /// <summary>401/403: this server's key was rejected. Nothing is counted against the events;
    /// sending stops until the probe authenticates again.</summary>
    AuthFailed,

    /// <summary>Any other non-success HTTP status (5xx, 408, 429, ...) or an unreadable body:
    /// a transient failure counted against each patient's first event.</summary>
    HttpError,

    /// <summary>This server is shutting down mid-call. Nothing is counted.</summary>
    Cancelled,
}

internal enum EventTransitionKind
{
    Synced,
    RetryScheduled,
    NeedsAttention,
    Conflict,
    BackToPending,
}

/// <summary>What happens to one event after a send.</summary>
internal sealed record EventTransition(
    PendingEvent Event,
    EventTransitionKind Kind,
    string Outcome,
    int RetryCount,
    TimeSpan? RetryDelay,
    string? ErrorCode,
    string? ErrorMessage,
    Guid? ReceiptId,
    DateTime? ProcessedAt,
    string? ConflictSnapshot,
    bool RecordAttempt)
{
    public string NewStatus => Kind switch
    {
        EventTransitionKind.Synced => SyncEventStatuses.Synced,
        EventTransitionKind.RetryScheduled => SyncEventStatuses.Failed,
        EventTransitionKind.NeedsAttention => SyncEventStatuses.NeedsAttention,
        EventTransitionKind.Conflict => SyncEventStatuses.Conflict,
        _ => SyncEventStatuses.Pending,
    };
}

/// <summary>What happens to one patient stream after a send.</summary>
internal sealed record StreamPlan(
    Guid StreamId,
    long? LastSyncedSequence,
    string HeadStatus,
    int HeadRetryCount,
    TimeSpan? NextAttemptDelay,
    string? LastErrorCode,
    IReadOnlyList<EventTransition> Transitions);

/// <summary>
/// Turns a send's outcome into per-event and per-stream transitions. Pure (no I/O), so the
/// ordering rules are unit-tested directly:
/// <list type="bullet">
/// <item>within a patient, events succeed in order up to the first non-success;</item>
/// <item>that event fails (retry, needs attention or conflict) and every later event of the same
/// patient goes back to Pending untouched, so Event 3 failing blocks Events 4 and 5;</item>
/// <item>other patients are planned independently and are never affected.</item>
/// </list>
/// </summary>
internal static class SyncResultPlanner
{
    public const string ConnectivityErrorCode = "SYNC.CLOUD_UNREACHABLE";
    public const string AuthFailedErrorCode = "SYNC.CLOUD_AUTH_FAILED";
    public const string CancelledErrorCode = "SYNC.CANCELLED";
    public const string HttpErrorCode = "SYNC.CLOUD_HTTP_ERROR";
    public const string MissingResultErrorCode = "SYNC.NO_RESULT";
    public const string RetriesExhaustedErrorCode = "SYNC.RETRIES_EXHAUSTED";

    private static readonly TimeSpan UnexplainedSkipBackoff = TimeSpan.FromSeconds(30);

    /// <param name="streams">Each stream's events, in sequence order.</param>
    /// <param name="resultsByEventId">The cloud's per-event results.</param>
    /// <param name="retryPolicy">Schedule for transient failures.</param>
    public static IReadOnlyList<StreamPlan> PlanResults(
        IReadOnlyList<IReadOnlyList<PendingEvent>> streams,
        IReadOnlyDictionary<Guid, SyncEventResult> resultsByEventId,
        SyncRetryPolicy retryPolicy)
    {
        var plans = new List<StreamPlan>(streams.Count);
        foreach (var events in streams)
        {
            var transitions = new List<EventTransition>(events.Count);
            long? lastSynced = null;
            EventTransition? stop = null;

            foreach (var pending in events)
            {
                if (stop is not null)
                {
                    transitions.Add(Pending(pending, SyncResultStatuses.NotAttempted, recordAttempt: false));
                    continue;
                }

                if (!resultsByEventId.TryGetValue(pending.EventId, out var result))
                {
                    stop = Pending(pending, SyncResultStatuses.NotAttempted, recordAttempt: false, MissingResultErrorCode, "The cloud returned no result for this event.");
                    transitions.Add(stop);
                    continue;
                }

                var transition = Plan(pending, result, retryPolicy);
                transitions.Add(transition);
                if (transition.Kind == EventTransitionKind.Synced)
                {
                    lastSynced = pending.SequenceNumber;
                }
                else
                {
                    stop = transition;
                }
            }

            plans.Add(ToStreamPlan(events[0].StreamId, lastSynced, stop, transitions));
        }

        return plans;
    }

    /// <summary>A whole-batch failure (no per-event results). Connectivity, auth and shutdown
    /// failures count nothing; an HTTP error counts one retry against each patient's first
    /// event only.</summary>
    public static IReadOnlyList<StreamPlan> PlanTransportFailure(
        IReadOnlyList<IReadOnlyList<PendingEvent>> streams,
        CloudTransportOutcome outcome,
        string? errorMessage,
        SyncRetryPolicy retryPolicy,
        TimeSpan connectivityBackoff)
    {
        var plans = new List<StreamPlan>(streams.Count);
        foreach (var events in streams)
        {
            var head = events[0];
            EventTransition headTransition;
            TimeSpan? streamDelay;

            if (outcome == CloudTransportOutcome.HttpError)
            {
                headTransition = Transient(head, retryPolicy, CloudTransportOutcome.HttpError.ToString(), HttpErrorCode, errorMessage);
                streamDelay = headTransition.RetryDelay;
            }
            else
            {
                var code = outcome switch
                {
                    CloudTransportOutcome.AuthFailed => AuthFailedErrorCode,
                    CloudTransportOutcome.Cancelled => CancelledErrorCode,
                    _ => ConnectivityErrorCode,
                };

                headTransition = Pending(head, outcome.ToString(), recordAttempt: outcome != CloudTransportOutcome.Cancelled, code, errorMessage);
                streamDelay = outcome == CloudTransportOutcome.Cancelled ? null : connectivityBackoff;
            }

            var transitions = new List<EventTransition>(events.Count) { headTransition };
            transitions.AddRange(events.Skip(1).Select(e => Pending(e, SyncResultStatuses.NotAttempted, recordAttempt: false)));

            var plan = ToStreamPlan(head.StreamId, null, headTransition, transitions);
            plans.Add(plan with { NextAttemptDelay = streamDelay });
        }

        return plans;
    }

    private static EventTransition Plan(PendingEvent pending, SyncEventResult result, SyncRetryPolicy retryPolicy)
    {
        switch (result.Status)
        {
            case SyncResultStatuses.Applied:
            case SyncResultStatuses.Duplicate:
            case SyncResultStatuses.Skipped:
                return new EventTransition(
                    pending, EventTransitionKind.Synced, result.Status, pending.RetryCount, null, null, null,
                    result.ReceiptId, result.ProcessedAt, null, RecordAttempt: true);

            case SyncResultStatuses.Rejected:
                return new EventTransition(
                    pending, EventTransitionKind.NeedsAttention, result.Status, pending.RetryCount, null,
                    result.ErrorCode ?? "SYNC.REJECTED", result.Message, null, null, null, RecordAttempt: true);

            case SyncResultStatuses.Conflict:
                return new EventTransition(
                    pending, EventTransitionKind.Conflict, result.Status, pending.RetryCount, null,
                    result.ErrorCode ?? "SYNC.CONFLICT", result.Message ?? "The cloud copy of this patient was changed outside sync.",
                    null, null, result.CloudSnapshot?.GetRawText(), RecordAttempt: true);

            case SyncResultStatuses.NotAttempted:
                return Pending(pending, result.Status, recordAttempt: false, result.ErrorCode, result.Message);

            default:
                // Transient, OutOfOrder, or a status this build does not know: retry with
                // backoff, which ends in NeedsAttention if it never clears.
                return Transient(pending, retryPolicy, result.Status, result.ErrorCode ?? "SYNC." + result.Status.ToUpperInvariant(), result.Message);
        }
    }

    private static EventTransition Transient(PendingEvent pending, SyncRetryPolicy retryPolicy, string outcome, string errorCode, string? message)
    {
        var decision = retryPolicy.OnTransientFailure(pending.RetryCount);
        return decision.Exhausted
            ? new EventTransition(
                pending, EventTransitionKind.NeedsAttention, outcome, decision.RetryCount, null,
                RetriesExhaustedErrorCode, $"Gave up after {decision.RetryCount - 1} retries. Last error: {errorCode}.", null, null, null, RecordAttempt: true)
            : new EventTransition(
                pending, EventTransitionKind.RetryScheduled, outcome, decision.RetryCount, decision.Delay,
                errorCode, message, null, null, null, RecordAttempt: true);
    }

    private static EventTransition Pending(PendingEvent pending, string outcome, bool recordAttempt, string? errorCode = null, string? message = null) =>
        new(pending, EventTransitionKind.BackToPending, outcome, pending.RetryCount, null, errorCode, message, null, null, null, recordAttempt);

    private static StreamPlan ToStreamPlan(Guid streamId, long? lastSynced, EventTransition? stop, IReadOnlyList<EventTransition> transitions)
    {
        if (stop is null)
        {
            // Everything sent was acknowledged. The store turns this into Pending if newer
            // events arrived meanwhile.
            return new StreamPlan(streamId, lastSynced, SyncEventStatuses.Synced, 0, null, null, transitions);
        }

        return new StreamPlan(
            streamId,
            lastSynced,
            stop.NewStatus,
            stop.RetryCount,
            stop.Kind switch
            {
                EventTransitionKind.RetryScheduled => stop.RetryDelay,
                // The cloud skipped the patient's first event without saying why: wait a little
                // rather than re-claiming the stream in a tight loop.
                EventTransitionKind.BackToPending => UnexplainedSkipBackoff,
                _ => null,
            },
            stop.ErrorCode,
            transitions);
    }
}
