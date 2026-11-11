using System.Diagnostics;
using System.Text.Json;
using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Sync.Contracts;
using HMS.Modules.Sync.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HMS.Modules.Sync.Application;

/// <summary>What one tenant run did (logged, and returned by the job for the Hangfire dashboard).</summary>
internal sealed record SyncRunSummary(string Outcome, int Batches, int EventsSent, int EventsSynced)
{
    public static SyncRunSummary Skipped(string reason) => new($"Skipped: {reason}", 0, 0, 0);
}

/// <summary>
/// One sync run for one hospital on an Edge server (ADR-133 §7). Must run in a DI scope whose
/// ITenantContext is already set to that hospital. Loops — claim streams, send one ordered batch,
/// write the results back — until nothing is claimable, a transport failure stops it, or
/// <c>MaxRunSeconds</c> is reached, so one run can drain a large backlog. All state lives in the
/// database; nothing survives in memory between runs, so a restart loses nothing.
/// </summary>
internal sealed class SyncTenantProcessor
{
    private readonly OutboxStore _store;
    private readonly ICloudSyncClient _cloud;
    private readonly SyncOptions _options;
    private readonly ILogger<SyncTenantProcessor> _logger;

    public SyncTenantProcessor(OutboxStore store, ICloudSyncClient cloud, IOptions<SyncOptions> options, ILogger<SyncTenantProcessor> logger)
    {
        _store = store;
        _cloud = cloud;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SyncRunSummary> RunAsync(TenantInfo tenant, CancellationToken cancellationToken)
    {
        if (!_options.IsEdge)
        {
            return SyncRunSummary.Skipped("this server is not an Edge server");
        }

        if (!_options.Enabled)
        {
            return SyncRunSummary.Skipped("sending is disabled (Sync:Enabled = false)");
        }

        var apiKey = _options.FindApiKey(tenant.HospitalCode);
        if (apiKey is null)
        {
            return SyncRunSummary.Skipped("no cloud key is configured for this hospital");
        }

        var state = await _store.GetStateAsync(cancellationToken);
        if (state.IsPaused)
        {
            return SyncRunSummary.Skipped("paused by an administrator");
        }

        if (state.AuthFailed)
        {
            return SyncRunSummary.Skipped("the cloud rejected this server's key; waiting for the probe to succeed");
        }

        if (state.CloudReachable == false)
        {
            return SyncRunSummary.Skipped("the cloud is unreachable; waiting for the probe to succeed");
        }

        var workerId = $"{_options.EffectiveNodeId}:{Environment.ProcessId}:{Guid.NewGuid():N}";
        if (workerId.Length > 100)
        {
            workerId = workerId[^100..];
        }

        var retryPolicy = SyncRetryPolicy.From(_options);
        var lease = TimeSpan.FromSeconds(_options.LeaseSeconds);
        var runClock = Stopwatch.StartNew();
        int batches = 0, sent = 0, synced = 0;
        bool? reachable = null;
        var authFailed = false;
        var outcome = "Completed";

        while (!cancellationToken.IsCancellationRequested && runClock.Elapsed.TotalSeconds < _options.MaxRunSeconds)
        {
            var claimed = await _store.ClaimStreamsAsync(workerId, _options.BatchSize, lease, cancellationToken);
            if (claimed.Count == 0)
            {
                break;
            }

            var selected = await SelectBatchAsync(claimed, workerId, cancellationToken);
            if (selected.Count == 0)
            {
                continue;
            }

            var events = selected.SelectMany(s => s).ToList();
            await _store.MarkProcessingAsync(events.Select(e => e.Id).ToList(), cancellationToken);

            var request = new SyncBatchRequest
            {
                SourceNodeId = _options.EffectiveNodeId,
                SourceEpoch = state.OutboxEpoch,
                Events = events.Select(ToEnvelope).ToList(),
            };

            var response = await _cloud.SendAsync(apiKey, request, cancellationToken);
            batches++;
            sent += events.Count;

            var plans = response.Outcome == CloudTransportOutcome.Success
                ? SyncResultPlanner.PlanResults(
                    selected,
                    response.Body!.Results.GroupBy(r => r.EventId).ToDictionary(g => g.Key, g => g.First()),
                    retryPolicy)
                : SyncResultPlanner.PlanTransportFailure(
                    selected,
                    response.Outcome,
                    response.Error,
                    retryPolicy,
                    TimeSpan.FromSeconds(_options.ConnectivityBackoffSeconds));

            // Written even while shutting down, so the outcome of a call that did happen is
            // never lost.
            var syncedNow = await _store.ApplyPlansAsync(plans, workerId, response.HttpStatus, response.DurationMs, CancellationToken.None);
            synced += syncedNow;

            LogBatch(tenant, response, events.Count, syncedNow, plans);

            if (response.Outcome != CloudTransportOutcome.Success)
            {
                outcome = response.Outcome.ToString();
                reachable = response.Outcome switch
                {
                    CloudTransportOutcome.Connectivity => false,
                    CloudTransportOutcome.Cancelled => null,
                    _ => true,
                };
                authFailed = response.Outcome == CloudTransportOutcome.AuthFailed;
                break;
            }

            reachable = true;
        }

        if (runClock.Elapsed.TotalSeconds >= _options.MaxRunSeconds && outcome == "Completed")
        {
            outcome = "TimeBudgetReached";
        }

        await _store.RecordRunAsync(synced > 0, reachable, authFailed, CancellationToken.None);

        var summary = new SyncRunSummary(outcome, batches, sent, synced);
        if (batches > 0)
        {
            _logger.LogInformation(
                "Sync run for hospital {HospitalCode}: {Outcome}, {Batches} batch(es), {Sent} event(s) sent, {Synced} synced, {ElapsedMs} ms.",
                tenant.HospitalCode,
                outcome,
                batches,
                sent,
                synced,
                runClock.ElapsedMilliseconds);
        }

        return summary;
    }

    /// <summary>
    /// Picks whole patient streams, in claim order, until the batch would exceed BatchSize events,
    /// and gives back the leases of the rest. A stream whose events are missing or do not continue
    /// from its last synced sequence is parked as NeedsAttention rather than sent out of order.
    /// </summary>
    private async Task<List<IReadOnlyList<PendingEvent>>> SelectBatchAsync(IReadOnlyList<ClaimedStream> claimed, string workerId, CancellationToken cancellationToken)
    {
        var events = await _store.LoadEventsAsync(claimed.Select(c => c.StreamId).ToList(), _options.MaxEventsPerStream, cancellationToken);
        var byStream = events.GroupBy(e => e.StreamId).ToDictionary(g => g.Key, g => (IReadOnlyList<PendingEvent>)g.OrderBy(e => e.SequenceNumber).ToList());

        var selected = new List<IReadOnlyList<PendingEvent>>();
        var release = new List<Guid>();
        var total = 0;

        foreach (var stream in claimed)
        {
            if (!byStream.TryGetValue(stream.StreamId, out var streamEvents) || !IsContiguous(stream.LastSyncedSequence, streamEvents))
            {
                _logger.LogError(
                    "Sync stream {StreamId} has a sequence gap after {LastSynced}; parked for an administrator.",
                    stream.StreamId,
                    stream.LastSyncedSequence);
                await _store.BlockStreamAsync(stream.StreamId, workerId, "SYNC.SEQUENCE_GAP", cancellationToken);
                continue;
            }

            if (selected.Count > 0 && total + streamEvents.Count > _options.BatchSize)
            {
                release.Add(stream.StreamId);
                continue;
            }

            selected.Add(streamEvents);
            total += streamEvents.Count;
        }

        await _store.ReleaseAsync(release, workerId, cancellationToken);
        return selected;
    }

    private static bool IsContiguous(long lastSynced, IReadOnlyList<PendingEvent> events)
    {
        if (events.Count == 0)
        {
            return false;
        }

        var expected = lastSynced + 1;
        foreach (var pending in events)
        {
            if (pending.SequenceNumber != expected)
            {
                return false;
            }

            expected++;
        }

        return true;
    }

    private static SyncEventEnvelope ToEnvelope(PendingEvent pending)
    {
        using var payload = JsonDocument.Parse(pending.Payload);
        return new SyncEventEnvelope
        {
            EventId = pending.EventId,
            StreamId = pending.StreamId,
            AggregateType = pending.AggregateType,
            AggregateId = pending.AggregateId,
            OperationType = pending.OperationType,
            SequenceNumber = pending.SequenceNumber,
            PayloadSchemaVersion = pending.PayloadSchemaVersion,
            PayloadHash = pending.PayloadHash,
            OccurredAt = DateTime.SpecifyKind(pending.CreatedAt, DateTimeKind.Utc),
            Payload = payload.RootElement.Clone(),
            Resolution = pending.Resolution,
        };
    }

    // Ids, counts and codes only: never payload values.
    private void LogBatch(TenantInfo tenant, CloudCallResult<SyncBatchResponse> response, int sent, int synced, IReadOnlyList<StreamPlan> plans)
    {
        if (response.Outcome == CloudTransportOutcome.Success)
        {
            foreach (var plan in plans.Where(p => p.HeadStatus is not (SyncEventStatuses.Synced or SyncEventStatuses.Pending)))
            {
                _logger.LogWarning(
                    "Sync: patient stream {StreamId} of hospital {HospitalCode} is {Status} ({ErrorCode}).",
                    plan.StreamId,
                    tenant.HospitalCode,
                    plan.HeadStatus,
                    plan.LastErrorCode);
            }

            _logger.LogDebug("Sync batch for {HospitalCode}: {Sent} sent, {Synced} synced in {DurationMs} ms.", tenant.HospitalCode, sent, synced, response.DurationMs);
            return;
        }

        _logger.LogWarning(
            "Sync batch for hospital {HospitalCode} failed: {Outcome} (HTTP {HttpStatus}) after {DurationMs} ms: {Error}",
            tenant.HospitalCode,
            response.Outcome,
            response.HttpStatus,
            response.DurationMs,
            response.Error);
    }
}
