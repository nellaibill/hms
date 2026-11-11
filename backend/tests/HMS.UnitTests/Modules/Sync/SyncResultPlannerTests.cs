using System.Text.Json;
using FluentAssertions;
using HMS.Modules.Sync.Application;
using HMS.Modules.Sync.Contracts;
using Xunit;

namespace HMS.UnitTests.Modules.Sync;

/// <summary>The per-patient ordering rules of ADR-133 §7.2: within a patient, events succeed in
/// order up to the first failure; later events wait; other patients are unaffected.</summary>
public class SyncResultPlannerTests
{
    private static readonly SyncRetryPolicy Retry = new([1, 2, 5, 10, 30], maxRetryCount: 5, random: () => 0.5);
    private static readonly TimeSpan Backoff = TimeSpan.FromSeconds(60);

    private static IReadOnlyList<PendingEvent> Stream(int count, long firstSequence = 1, int headRetryCount = 0)
    {
        var streamId = Guid.NewGuid();
        return Enumerable.Range(0, count)
            .Select(i => new PendingEvent(
                Guid.NewGuid(), Guid.NewGuid(), streamId, "Patient", streamId, "Update", firstSequence + i,
                "{}", 1, "hash", DateTime.UtcNow, i == 0 ? headRetryCount : 0, null))
            .ToList();
    }

    private static SyncEventResult Result(PendingEvent e, string status, string? code = null) =>
        new() { EventId = e.EventId, Status = status, ErrorCode = code, ReceiptId = status == SyncResultStatuses.Applied ? Guid.NewGuid() : null };

    [Fact]
    public void AllApplied_SyncsEveryEventAndTheStream()
    {
        var stream = Stream(3);
        var results = stream.ToDictionary(e => e.EventId, e => Result(e, SyncResultStatuses.Applied));

        var plan = SyncResultPlanner.PlanResults([stream], results, Retry).Single();

        plan.Transitions.Should().OnlyContain(t => t.Kind == EventTransitionKind.Synced);
        plan.LastSyncedSequence.Should().Be(3);
        plan.HeadStatus.Should().Be(SyncEventStatuses.Synced);
        plan.NextAttemptDelay.Should().BeNull();
    }

    [Fact]
    public void DuplicateAndSkipped_CountAsSynced()
    {
        var stream = Stream(2);
        var results = new Dictionary<Guid, SyncEventResult>
        {
            [stream[0].EventId] = Result(stream[0], SyncResultStatuses.Duplicate),
            [stream[1].EventId] = Result(stream[1], SyncResultStatuses.Skipped),
        };

        var plan = SyncResultPlanner.PlanResults([stream], results, Retry).Single();

        plan.Transitions.Should().OnlyContain(t => t.Kind == EventTransitionKind.Synced);
        plan.LastSyncedSequence.Should().Be(2);
    }

    [Fact]
    public void FiveUpdates_ThirdFailsTransiently_FirstTwoSynced_ThirdRetries_FourAndFiveWait()
    {
        var stream = Stream(5);
        var results = new Dictionary<Guid, SyncEventResult>
        {
            [stream[0].EventId] = Result(stream[0], SyncResultStatuses.Applied),
            [stream[1].EventId] = Result(stream[1], SyncResultStatuses.Applied),
            [stream[2].EventId] = Result(stream[2], SyncResultStatuses.Transient, "SYNC.CLOUD_DB_UNAVAILABLE"),
            [stream[3].EventId] = Result(stream[3], SyncResultStatuses.NotAttempted),
            [stream[4].EventId] = Result(stream[4], SyncResultStatuses.NotAttempted),
        };

        var plan = SyncResultPlanner.PlanResults([stream], results, Retry).Single();

        plan.Transitions.Select(t => t.Kind).Should().Equal(
            EventTransitionKind.Synced,
            EventTransitionKind.Synced,
            EventTransitionKind.RetryScheduled,
            EventTransitionKind.BackToPending,
            EventTransitionKind.BackToPending);
        plan.LastSyncedSequence.Should().Be(2);
        plan.HeadStatus.Should().Be(SyncEventStatuses.Failed);
        plan.HeadRetryCount.Should().Be(1);
        plan.NextAttemptDelay.Should().Be(TimeSpan.FromMinutes(1));
        plan.LastErrorCode.Should().Be("SYNC.CLOUD_DB_UNAVAILABLE");
        plan.Transitions[3].RetryCount.Should().Be(0, "a waiting event keeps its retry count");
        plan.Transitions[3].RecordAttempt.Should().BeFalse();
    }

    [Fact]
    public void OneFailingPatient_DoesNotAffectAnother()
    {
        var failing = Stream(2);
        var healthy = Stream(2);
        var results = new Dictionary<Guid, SyncEventResult>
        {
            [failing[0].EventId] = Result(failing[0], SyncResultStatuses.Rejected, "PATIENT_SYNC.UNKNOWN_DISTRICT"),
            [failing[1].EventId] = Result(failing[1], SyncResultStatuses.NotAttempted),
            [healthy[0].EventId] = Result(healthy[0], SyncResultStatuses.Applied),
            [healthy[1].EventId] = Result(healthy[1], SyncResultStatuses.Applied),
        };

        var plans = SyncResultPlanner.PlanResults([failing, healthy], results, Retry);

        plans[0].HeadStatus.Should().Be(SyncEventStatuses.NeedsAttention);
        plans[0].LastSyncedSequence.Should().BeNull();
        plans[0].NextAttemptDelay.Should().BeNull("a permanent error is not retried automatically");
        plans[0].LastErrorCode.Should().Be("PATIENT_SYNC.UNKNOWN_DISTRICT");
        plans[1].HeadStatus.Should().Be(SyncEventStatuses.Synced);
        plans[1].LastSyncedSequence.Should().Be(2);
    }

    [Fact]
    public void Conflict_BlocksThePatientAndKeepsTheCloudVersion()
    {
        var stream = Stream(2);
        using var cloud = JsonDocument.Parse("""{"id":"x"}""");
        var results = new Dictionary<Guid, SyncEventResult>
        {
            [stream[0].EventId] = new() { EventId = stream[0].EventId, Status = SyncResultStatuses.Conflict, CloudSnapshot = cloud.RootElement.Clone() },
            [stream[1].EventId] = Result(stream[1], SyncResultStatuses.NotAttempted),
        };

        var plan = SyncResultPlanner.PlanResults([stream], results, Retry).Single();

        plan.HeadStatus.Should().Be(SyncEventStatuses.Conflict);
        plan.Transitions[0].Kind.Should().Be(EventTransitionKind.Conflict);
        plan.Transitions[0].ConflictSnapshot.Should().Be("""{"id":"x"}""");
        plan.Transitions[1].Kind.Should().Be(EventTransitionKind.BackToPending);
    }

    [Fact]
    public void OutOfOrder_IsRetriedWithBackoff()
    {
        var stream = Stream(1);
        var results = new Dictionary<Guid, SyncEventResult> { [stream[0].EventId] = Result(stream[0], SyncResultStatuses.OutOfOrder) };

        var plan = SyncResultPlanner.PlanResults([stream], results, Retry).Single();

        plan.HeadStatus.Should().Be(SyncEventStatuses.Failed);
        plan.LastErrorCode.Should().Be("SYNC.OUTOFORDER");
    }

    [Fact]
    public void TransientFailure_AfterMaxRetries_NeedsAttention()
    {
        var stream = Stream(1, headRetryCount: 5);
        var results = new Dictionary<Guid, SyncEventResult> { [stream[0].EventId] = Result(stream[0], SyncResultStatuses.Transient) };

        var plan = SyncResultPlanner.PlanResults([stream], results, Retry).Single();

        plan.HeadStatus.Should().Be(SyncEventStatuses.NeedsAttention);
        plan.LastErrorCode.Should().Be(SyncResultPlanner.RetriesExhaustedErrorCode);
    }

    [Fact]
    public void MissingResult_PutsTheEventsBackWithAShortBackoff()
    {
        var stream = Stream(2);

        var plan = SyncResultPlanner.PlanResults([stream], new Dictionary<Guid, SyncEventResult>(), Retry).Single();

        plan.Transitions.Should().OnlyContain(t => t.Kind == EventTransitionKind.BackToPending);
        plan.HeadStatus.Should().Be(SyncEventStatuses.Pending);
        plan.NextAttemptDelay.Should().NotBeNull();
    }

    [Theory]
    [InlineData(nameof(CloudTransportOutcome.Connectivity), SyncResultPlanner.ConnectivityErrorCode)]
    [InlineData(nameof(CloudTransportOutcome.AuthFailed), SyncResultPlanner.AuthFailedErrorCode)]
    public void ConnectivityOrAuthFailure_CountsNoRetry(string outcomeName, string code)
    {
        var outcome = Enum.Parse<CloudTransportOutcome>(outcomeName);
        var stream = Stream(3, headRetryCount: 2);

        var plan = SyncResultPlanner.PlanTransportFailure([stream], outcome, "boom", Retry, Backoff).Single();

        plan.Transitions.Should().OnlyContain(t => t.Kind == EventTransitionKind.BackToPending);
        plan.HeadRetryCount.Should().Be(2, "time offline must not use up retries");
        plan.HeadStatus.Should().Be(SyncEventStatuses.Pending);
        plan.NextAttemptDelay.Should().Be(Backoff);
        plan.LastErrorCode.Should().Be(code);
        plan.Transitions[0].RecordAttempt.Should().BeTrue();
    }

    [Fact]
    public void Cancelled_CountsNothingAndRetriesImmediately()
    {
        var plan = SyncResultPlanner.PlanTransportFailure([Stream(1)], CloudTransportOutcome.Cancelled, null, Retry, Backoff).Single();

        plan.NextAttemptDelay.Should().BeNull();
        plan.Transitions[0].RecordAttempt.Should().BeFalse();
    }

    [Fact]
    public void HttpError_CountsOneRetryAgainstEachPatientsFirstEventOnly()
    {
        var first = Stream(3);
        var second = Stream(1, headRetryCount: 1);

        var plans = SyncResultPlanner.PlanTransportFailure([first, second], CloudTransportOutcome.HttpError, "HTTP 500", Retry, Backoff);

        plans[0].Transitions[0].Kind.Should().Be(EventTransitionKind.RetryScheduled);
        plans[0].Transitions[0].RetryCount.Should().Be(1);
        plans[0].Transitions.Skip(1).Should().OnlyContain(t => t.Kind == EventTransitionKind.BackToPending && t.RetryCount == 0);
        plans[0].NextAttemptDelay.Should().Be(TimeSpan.FromMinutes(1));
        plans[1].Transitions[0].RetryCount.Should().Be(2);
        plans[1].NextAttemptDelay.Should().Be(TimeSpan.FromMinutes(2));
    }
}
