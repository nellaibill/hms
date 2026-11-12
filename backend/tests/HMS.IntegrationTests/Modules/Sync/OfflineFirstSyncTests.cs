using FluentAssertions;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Sync.Application;
using HMS.Modules.Sync.Contracts;
using HMS.Modules.Sync.Infrastructure;
using HMS.Shared.Kernel.Sync;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HMS.IntegrationTests.Modules.Sync;

/// <summary>
/// Offline-first sync (ADR-133) against real PostgreSQL databases: one local (Edge) hospital
/// database and one cloud database, using the real Patients, Masters and Sync code. See
/// <see cref="SyncTestEnvironment"/> for how the database server is chosen.
/// </summary>
public class OfflineFirstSyncTests : IClassFixture<SyncTestEnvironment>, IAsyncLifetime
{
    private readonly SyncTestEnvironment _env;

    public OfflineFirstSyncTests(SyncTestEnvironment env)
    {
        _env = env;
    }

    private string Edge => _env.EdgeConnectionString;
    private string Cloud => _env.CloudConnectionString;

    public Task InitializeAsync() => _env.ResetEdgeStateAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private Task<List<Dictionary<string, object?>>> EventsAsync(Guid patientId) =>
        SyncTestEnvironment.RowsAsync(Edge, $"SELECT sequence_number, operation_type, status, retry_count, changed_fields, payload_hash, encode(sha256(convert_to(payload::text, 'UTF8')), 'hex') AS computed_hash, error_code FROM sync.outbox_events WHERE stream_id = '{patientId}' ORDER BY sequence_number");

    private Task<string?> EdgeStatusAsync(Guid patientId, long sequence) =>
        SyncTestEnvironment.ScalarAsync<string>(Edge, $"SELECT status FROM sync.outbox_events WHERE stream_id = '{patientId}' AND sequence_number = {sequence}");

    private Task<string?> CloudFirstNameAsync(Guid patientId) =>
        SyncTestEnvironment.ScalarAsync<string>(Cloud, $"SELECT first_name FROM patients.patients WHERE id = '{patientId}'");

    // ---- Capture (write path) ------------------------------------------------------------

    [Fact]
    public async Task EveryPatientOperation_WritesExactlyOneOrderedOutboxEvent_WithFieldNamesOnly()
    {
        var created = await _env.CreatePatientAsync("Capture");
        await _env.UpdatePatientAsync(created.Id, r => r with { FirstName = "Captured" });

        await using (var scope = _env.EdgeScope())
        {
            var patients = scope.ServiceProvider.GetRequiredService<IPatientService>();
            (await patients.AddAllergyAsync(created.Id, new AddAllergyRequest { AllergyType = AllergyType.Food, Severity = AllergySeverity.Mild }, null, CancellationToken.None)).IsSuccess.Should().BeTrue();
            (await patients.DeleteAsync(created.Id, null, CancellationToken.None)).IsSuccess.Should().BeTrue();
        }

        var events = await EventsAsync(created.Id);

        events.Select(e => (long)e["sequence_number"]!).Should().Equal(1, 2, 3, 4);
        events.Select(e => (string)e["operation_type"]!).Should().Equal("Insert", "Update", "Update", "Delete");
        events.Should().OnlyContain(e => (string)e["status"]! == "Pending");
        events.Should().OnlyContain(e => (string)e["payload_hash"]! == (string)e["computed_hash"]!);
        ((string[])events[1]["changed_fields"]!).Should().Equal("FirstName");
        ((string[])events[2]["changed_fields"]!).Should().Contain("Allergies");
        string.Join(",", (string[])events[1]["changed_fields"]!).Should().NotContain("Captured", "only field names are stored outside the payload");

        (await SyncTestEnvironment.ScalarAsync<long>(Edge, $"SELECT last_sequence FROM sync.outbox_streams WHERE stream_id = '{created.Id}'")).Should().Be(4);
    }

    [Fact]
    public async Task FailedSave_LeavesNeitherThePatientNorAnOutboxEvent()
    {
        var first = await _env.CreatePatientAsync("Unique");
        var eventsBefore = await SyncTestEnvironment.ScalarAsync<long>(Edge, "SELECT count(*) FROM sync.outbox_events");
        var uhid = first.Uhid;

        await using (var scope = _env.EdgeScope())
        {
            // Same UHID as an existing patient: the unique index makes the save itself fail.
            var act = () => scope.ServiceProvider.GetRequiredService<IPatientService>()
                .CreateAsync(_env.NewPatientRequest("Clash"), null, CancellationToken.None, uhidOverride: uhid);
            await act.Should().ThrowAsync<Exception>();
        }

        (await SyncTestEnvironment.ScalarAsync<long>(Edge, "SELECT count(*) FROM sync.outbox_events")).Should().Be(eventsBefore);
        (await SyncTestEnvironment.ScalarAsync<long>(Edge, $"SELECT count(*) FROM patients.patients WHERE uhid = '{uhid}'")).Should().Be(1);
        (await SyncTestEnvironment.ScalarAsync<long>(Edge, "SELECT count(*) FROM sync.outbox_streams s WHERE NOT EXISTS (SELECT 1 FROM patients.patients p WHERE p.id = s.stream_id)"))
            .Should().Be(0, "a rolled-back save must not leave a stream behind");
    }

    [Fact]
    public async Task ConcurrentEditsOfOnePatient_GetGapFreeSequenceNumbers()
    {
        var patient = await _env.CreatePatientAsync("Concurrent");

        var attempts = Enumerable.Range(0, 8).Select(async i =>
        {
            await using var scope = _env.EdgeScope();
            try
            {
                var result = await scope.ServiceProvider.GetRequiredService<IPatientService>()
                    .AddAllergyAsync(patient.Id, new AddAllergyRequest { AllergyType = AllergyType.Food, Specify = "Item" + i, Severity = AllergySeverity.Mild }, null, CancellationToken.None);
                return result.IsSuccess;
            }
            catch
            {
                return false; // an optimistic-concurrency loser rolls back entirely
            }
        });
        var succeeded = (await Task.WhenAll(attempts)).Count(ok => ok);

        var sequences = (await EventsAsync(patient.Id)).Select(e => (long)e["sequence_number"]!).ToList();
        succeeded.Should().BeGreaterThan(0);
        sequences.Should().Equal(Enumerable.Range(1, succeeded + 1).Select(n => (long)n), "one event per successful save, numbered without gaps");
    }

    // ---- Claiming --------------------------------------------------------------------------

    [Fact]
    public async Task TwoWorkers_NeverClaimTheSamePatient_AndAnExpiredLeaseIsRecovered()
    {
        // Park every other stream so only this test's patients are claimable.
        await SyncTestEnvironment.ExecuteAsync(Edge, "UPDATE sync.outbox_streams SET next_attempt_at = now() + interval '1 hour' WHERE last_synced_sequence < last_sequence");
        var ids = new List<Guid>();
        for (var i = 0; i < 6; i++)
        {
            ids.Add((await _env.CreatePatientAsync("Claim" + i)).Id);
        }

        await using var scopeA = _env.EdgeScope();
        await using var scopeB = _env.EdgeScope();
        var storeA = scopeA.ServiceProvider.GetRequiredService<OutboxStore>();
        var storeB = scopeB.ServiceProvider.GetRequiredService<OutboxStore>();

        var claims = await Task.WhenAll(
            storeA.ClaimStreamsAsync("worker-a", 3, TimeSpan.FromMinutes(2), CancellationToken.None),
            storeB.ClaimStreamsAsync("worker-b", 3, TimeSpan.FromMinutes(2), CancellationToken.None));

        var a = claims[0].Select(c => c.StreamId).ToList();
        var b = claims[1].Select(c => c.StreamId).ToList();
        a.Intersect(b).Should().BeEmpty();
        a.Concat(b).Should().BeEquivalentTo(ids);
        (await storeB.ClaimStreamsAsync("worker-b", 10, TimeSpan.FromMinutes(2), CancellationToken.None)).Should().BeEmpty("live leases are respected");

        // Worker A "crashes": its lease runs out and another worker takes its patients over.
        await SyncTestEnvironment.ExecuteAsync(Edge, $"UPDATE sync.outbox_streams SET lease_expires_at = now() - interval '1 second' WHERE lease_owner = 'worker-a'");
        var recovered = await storeB.ClaimStreamsAsync("worker-c", 10, TimeSpan.FromMinutes(2), CancellationToken.None);
        recovered.Select(c => c.StreamId).Should().BeEquivalentTo(a);

        await storeA.ReleaseAsync(ids, "worker-b", CancellationToken.None);
        await storeA.ReleaseAsync(ids, "worker-c", CancellationToken.None);
    }

    // ---- End to end --------------------------------------------------------------------------

    [Fact]
    public async Task PendingChanges_ReachTheCloud_InOrder_AndAreAcknowledged()
    {
        var patient = await _env.CreatePatientAsync("Alpha");
        await _env.UpdatePatientAsync(patient.Id, r => r with { FirstName = "Alpha2" });
        await _env.UpdatePatientAsync(patient.Id, r => r with { FirstName = "Alpha3" });

        var run = await _env.RunSyncAsync();

        run.Synced.Should().BeGreaterThanOrEqualTo(3);
        (await EventsAsync(patient.Id)).Should().OnlyContain(e => (string)e["status"]! == "Synced");
        (await CloudFirstNameAsync(patient.Id)).Should().Be("Alpha3");
        (await SyncTestEnvironment.ScalarAsync<long>(Cloud, $"SELECT last_applied_sequence FROM sync.replica_streams WHERE stream_id = '{patient.Id}'")).Should().Be(3);
        (await SyncTestEnvironment.ScalarAsync<long>(Cloud, $"SELECT count(*) FROM sync.inbox_events WHERE stream_id = '{patient.Id}'")).Should().Be(3);
        (await SyncTestEnvironment.ScalarAsync<long>(Edge, $"SELECT count(*) FROM sync.outbox_events WHERE stream_id = '{patient.Id}' AND cloud_receipt_id IS NULL")).Should().Be(0);

        // The cloud copy is an exact replica: same ids, same audit columns, same children.
        await using var edgeScope = _env.EdgeScope();
        await using var cloudScope = _env.CloudScope();
        var edgeSnapshot = await SnapshotAsync(edgeScope.ServiceProvider, Edge, patient.Id);
        var cloudSnapshot = await SnapshotAsync(cloudScope.ServiceProvider, Cloud, patient.Id);
        cloudSnapshot.Should().Be(edgeSnapshot);
    }

    [Fact]
    public async Task ResendingAnAcknowledgedEvent_IsIdempotent()
    {
        var patient = await _env.CreatePatientAsync("Idem");
        await _env.RunSyncAsync();
        var inboxBefore = await SyncTestEnvironment.ScalarAsync<long>(Cloud, "SELECT count(*) FROM sync.inbox_events");
        var xminBefore = await SyncTestEnvironment.ScalarAsync<string>(Cloud, $"SELECT xmin::text FROM patients.patients WHERE id = '{patient.Id}'");

        // The acknowledgement was "lost": the local server still thinks the event is unsent.
        await SyncTestEnvironment.ExecuteAsync(Edge, $"""
            UPDATE sync.outbox_events SET status = 'Pending', synced_at = NULL WHERE stream_id = '{patient.Id}';
            UPDATE sync.outbox_streams SET last_synced_sequence = 0, status = 'Pending' WHERE stream_id = '{patient.Id}';
            """);

        await _env.RunSyncAsync();

        (await EdgeStatusAsync(patient.Id, 1)).Should().Be("Synced");
        (await SyncTestEnvironment.ScalarAsync<long>(Cloud, "SELECT count(*) FROM sync.inbox_events")).Should().Be(inboxBefore, "no second inbox row");
        (await SyncTestEnvironment.ScalarAsync<string>(Cloud, $"SELECT xmin::text FROM patients.patients WHERE id = '{patient.Id}'")).Should().Be(xminBefore, "the patient was not written again");
        (await SyncTestEnvironment.ScalarAsync<string>(Edge, $"SELECT outcome FROM sync.outbox_attempts a JOIN sync.outbox_events e ON e.event_id = a.event_id WHERE e.stream_id = '{patient.Id}' ORDER BY a.attempt_no DESC LIMIT 1"))
            .Should().Be("Duplicate");
    }

    [Fact]
    public async Task EventsLeftProcessingByACrash_AreResentAfterTheLeaseExpires()
    {
        var patient = await _env.CreatePatientAsync("Crash");
        await SyncTestEnvironment.ExecuteAsync(Edge, $"""
            UPDATE sync.outbox_events SET status = 'Processing' WHERE stream_id = '{patient.Id}';
            UPDATE sync.outbox_streams SET lease_owner = 'dead-worker', lease_expires_at = now() - interval '1 second' WHERE stream_id = '{patient.Id}';
            """);

        await _env.RunSyncAsync();

        (await EdgeStatusAsync(patient.Id, 1)).Should().Be("Synced");
        (await CloudFirstNameAsync(patient.Id)).Should().Be("Crash");
    }

    // ---- Failures ------------------------------------------------------------------------

    [Fact]
    public async Task Offline_KeepsEventsPending_WithoutUsingRetries_ThenRecovers()
    {
        var patient = await _env.CreatePatientAsync("Offline");
        _env.CloudHandler.Mode = CloudMode.Offline;

        var run = await _env.RunSyncAsync();

        run.Outcome.Should().Be("Connectivity");
        var events = await EventsAsync(patient.Id);
        events.Single()["status"].Should().Be("Pending");
        events.Single()["retry_count"].Should().Be(0);
        (await SyncTestEnvironment.ScalarAsync<bool>(Edge, "SELECT cloud_reachable FROM sync.sync_state")).Should().BeFalse();
        (await _env.RunSyncAsync()).Outcome.Should().StartWith("Skipped", "sending waits for the probe while the cloud is unreachable");

        // Internet back: the probe flips the state, the back-off passes, and the run drains.
        _env.CloudHandler.Mode = CloudMode.Online;
        await using (var scope = _env.EdgeScope())
        {
            var ping = await scope.ServiceProvider.GetRequiredService<ICloudSyncClient>().PingAsync(SyncTestEnvironment.ApiKey, CancellationToken.None);
            await scope.ServiceProvider.GetRequiredService<OutboxStore>().RecordProbeAsync(ping.Outcome == CloudTransportOutcome.Success, false, null, ping.DurationMs, CancellationToken.None);
        }

        await SyncTestEnvironment.ExecuteAsync(Edge, $"UPDATE sync.outbox_streams SET next_attempt_at = now() WHERE stream_id = '{patient.Id}'");
        await _env.RunSyncAsync();

        (await EdgeStatusAsync(patient.Id, 1)).Should().Be("Synced");
    }

    [Fact]
    public async Task Timeout_IsTreatedAsConnectivity_AndCountsNoRetry()
    {
        var patient = await _env.CreatePatientAsync("Slow");
        _env.CloudHandler.Mode = CloudMode.Timeout;

        (await _env.RunSyncAsync()).Outcome.Should().Be("Connectivity");

        var head = (await EventsAsync(patient.Id)).Single();
        head["status"].Should().Be("Pending");
        head["retry_count"].Should().Be(0);
    }

    [Fact]
    public async Task ServerError_CountsOneRetryAgainstTheFirstEventOnly()
    {
        var patient = await _env.CreatePatientAsync("Err");
        await _env.UpdatePatientAsync(patient.Id, r => r with { FirstName = "Err2" });
        _env.CloudHandler.Mode = CloudMode.ServerError;

        (await _env.RunSyncAsync()).Outcome.Should().Be("HttpError");

        var events = await EventsAsync(patient.Id);
        events[0]["status"].Should().Be("Failed");
        events[0]["retry_count"].Should().Be(1);
        events[1]["status"].Should().Be("Pending");
        events[1]["retry_count"].Should().Be(0);
        (await SyncTestEnvironment.ScalarAsync<bool>(Edge, $"SELECT next_attempt_at > now() + interval '40 seconds' FROM sync.outbox_streams WHERE stream_id = '{patient.Id}'"))
            .Should().BeTrue("the first retry waits about a minute");
    }

    [Fact]
    public async Task RejectedKey_PausesSendingWithoutUsingRetries()
    {
        var patient = await _env.CreatePatientAsync("Key");
        _env.CloudHandler.Mode = CloudMode.Unauthorized;

        (await _env.RunSyncAsync()).Outcome.Should().Be("AuthFailed");

        (await EventsAsync(patient.Id)).Single()["retry_count"].Should().Be(0);
        (await SyncTestEnvironment.ScalarAsync<bool>(Edge, "SELECT auth_failed FROM sync.sync_state")).Should().BeTrue();
        (await _env.RunSyncAsync()).Outcome.Should().StartWith("Skipped");
    }

    [Fact]
    public async Task FiveUpdates_ThirdRejected_BlocksOnlyThatPatient_UntilSkipped()
    {
        var bystander = await _env.CreatePatientAsync("Bystander");
        var patient = await _env.CreatePatientAsync("Blocked");
        await _env.UpdatePatientAsync(patient.Id, r => r with { FirstName = "Blocked2" });
        // #3 points at a district the cloud does not have: a permanent error there.
        await _env.UpdatePatientAsync(patient.Id, r => r with { Address = r.Address with { DistrictId = _env.EdgeOnlyDistrictId } });
        await _env.UpdatePatientAsync(patient.Id, r => r with { Address = r.Address with { DistrictId = _env.DistrictId } });
        await _env.UpdatePatientAsync(patient.Id, r => r with { FirstName = "Blocked5" });

        await _env.RunSyncAsync();

        var statuses = (await EventsAsync(patient.Id)).Select(e => (string)e["status"]!).ToList();
        statuses.Should().Equal("Synced", "Synced", "NeedsAttention", "Pending", "Pending");
        (await EventsAsync(patient.Id))[2]["error_code"].Should().Be(PatientSyncErrorCodes.InvalidDistrict);
        (await CloudFirstNameAsync(patient.Id)).Should().Be("Blocked2");
        (await EdgeStatusAsync(bystander.Id, 1)).Should().Be("Synced", "other patients keep synchronising");

        // Another run changes nothing: a blocked patient waits for an administrator.
        await _env.RunSyncAsync();
        (await EdgeStatusAsync(patient.Id, 3)).Should().Be("NeedsAttention");

        Guid eventId;
        await using (var scope = _env.EdgeScope())
        {
            eventId = (await SyncTestEnvironment.ScalarAsync<Guid?>(Edge, $"SELECT event_id FROM sync.outbox_events WHERE stream_id = '{patient.Id}' AND sequence_number = 3")).GetValueOrDefault();
            var result = await scope.ServiceProvider.GetRequiredService<ISyncMonitorService>()
                .ResolveAsync(eventId, new SyncResolveRequest { Resolution = SyncResolutions.Skip, Note = "district missing in cloud" }, Guid.NewGuid(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue(result.Error);
        }

        await _env.RunSyncAsync();

        (await EventsAsync(patient.Id)).Should().OnlyContain(e => (string)e["status"]! == "Synced");
        (await CloudFirstNameAsync(patient.Id)).Should().Be("Blocked5");
        (await SyncTestEnvironment.ScalarAsync<string>(Cloud, $"SELECT status FROM sync.inbox_events WHERE event_id = '{eventId}'")).Should().Be("Skipped");
    }

    [Fact]
    public async Task ManualRetry_ResetsAFailedEvent()
    {
        var patient = await _env.CreatePatientAsync("Retry");
        _env.CloudHandler.Mode = CloudMode.ServerError;
        await _env.RunSyncAsync();
        _env.CloudHandler.Mode = CloudMode.Online;

        var eventId = (await SyncTestEnvironment.ScalarAsync<Guid?>(Edge, $"SELECT event_id FROM sync.outbox_events WHERE stream_id = '{patient.Id}'")).GetValueOrDefault();
        await using (var scope = _env.EdgeScope())
        {
            var result = await scope.ServiceProvider.GetRequiredService<ISyncMonitorService>().RetryEventAsync(eventId, Guid.NewGuid(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue(result.Error);
        }

        (await EventsAsync(patient.Id)).Single()["retry_count"].Should().Be(0);
        await _env.RunSyncAsync();
        (await EdgeStatusAsync(patient.Id, 1)).Should().Be("Synced");
    }

    // ---- Conflicts -------------------------------------------------------------------------

    [Fact]
    public async Task CloudEditOutsideSync_IsAConflict_NotOverwritten_ThenLocalWins()
    {
        var patient = await _env.CreatePatientAsync("Conf");
        await _env.RunSyncAsync();
        await SyncTestEnvironment.ExecuteAsync(Cloud, $"UPDATE patients.patients SET first_name = 'CloudEdit' WHERE id = '{patient.Id}'");
        await _env.UpdatePatientAsync(patient.Id, r => r with { FirstName = "LocalEdit" });

        await _env.RunSyncAsync();

        (await EdgeStatusAsync(patient.Id, 2)).Should().Be("Conflict");
        (await CloudFirstNameAsync(patient.Id)).Should().Be("CloudEdit", "no last-write-wins");
        (await SyncTestEnvironment.ScalarAsync<string>(Cloud, $"SELECT status FROM sync.conflicts WHERE stream_id = '{patient.Id}'")).Should().Be("Open");

        var eventId = (await SyncTestEnvironment.ScalarAsync<Guid?>(Edge, $"SELECT event_id FROM sync.outbox_events WHERE stream_id = '{patient.Id}' AND sequence_number = 2")).GetValueOrDefault();
        await using (var scope = _env.EdgeScope())
        {
            (await scope.ServiceProvider.GetRequiredService<ISyncMonitorService>()
                .ResolveAsync(eventId, new SyncResolveRequest { Resolution = SyncResolutions.LocalWins }, Guid.NewGuid(), CancellationToken.None)).IsSuccess.Should().BeTrue();
        }

        await _env.RunSyncAsync();

        (await EdgeStatusAsync(patient.Id, 2)).Should().Be("Synced");
        (await CloudFirstNameAsync(patient.Id)).Should().Be("LocalEdit");
        (await SyncTestEnvironment.ScalarAsync<string>(Cloud, $"SELECT status FROM sync.conflicts WHERE stream_id = '{patient.Id}'")).Should().Be("ResolvedLocalWins");
    }

    [Fact]
    public async Task Conflict_ResolvedCloudWins_LeavesBothSidesIdentical()
    {
        var patient = await _env.CreatePatientAsync("CloudWins");
        await _env.RunSyncAsync();
        await SyncTestEnvironment.ExecuteAsync(Cloud, $"UPDATE patients.patients SET first_name = 'FromCloud' WHERE id = '{patient.Id}'");
        await _env.UpdatePatientAsync(patient.Id, r => r with { LastName = "LocalChange" });
        await _env.RunSyncAsync();
        (await EdgeStatusAsync(patient.Id, 2)).Should().Be("Conflict");

        var eventId = (await SyncTestEnvironment.ScalarAsync<Guid?>(Edge, $"SELECT event_id FROM sync.outbox_events WHERE stream_id = '{patient.Id}' AND sequence_number = 2")).GetValueOrDefault();
        await using (var scope = _env.EdgeScope())
        {
            var result = await scope.ServiceProvider.GetRequiredService<ISyncMonitorService>()
                .ResolveAsync(eventId, new SyncResolveRequest { Resolution = SyncResolutions.CloudWins }, Guid.NewGuid(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue(result.Error);
        }

        (await SyncTestEnvironment.ScalarAsync<string>(Edge, $"SELECT first_name FROM patients.patients WHERE id = '{patient.Id}'")).Should().Be("FromCloud");

        await _env.RunSyncAsync();

        (await EventsAsync(patient.Id)).Should().OnlyContain(e => (string)e["status"]! == "Synced");
        await using var edgeScope = _env.EdgeScope();
        await using var cloudScope = _env.CloudScope();
        (await SnapshotAsync(cloudScope.ServiceProvider, Cloud, patient.Id)).Should().Be(await SnapshotAsync(edgeScope.ServiceProvider, Edge, patient.Id));
        (await SyncTestEnvironment.ScalarAsync<string>(Cloud, $"SELECT status FROM sync.conflicts WHERE stream_id = '{patient.Id}'")).Should().Be("ResolvedCloudWins");
    }

    // ---- Backfill --------------------------------------------------------------------------

    [Fact]
    public async Task Backfill_QueuesOnlyPatientsThatWereNeverSynced()
    {
        var captured = await _env.CreatePatientAsync("Captured");
        var legacy = await _env.CreatePatientAsync("Legacy");
        // Pretend "legacy" existed before capture was switched on.
        await SyncTestEnvironment.ExecuteAsync(Edge, $"""
            DELETE FROM sync.outbox_events WHERE stream_id = '{legacy.Id}';
            DELETE FROM sync.outbox_streams WHERE stream_id = '{legacy.Id}';
            """);

        int written;
        await using (var scope = _env.EdgeScope())
        {
            var writer = scope.ServiceProvider.GetRequiredService<IOutboxWriter>();
            written = await scope.ServiceProvider.GetRequiredService<IPatientSyncService>().BackfillOutboxAsync(writer, 2, CancellationToken.None);
        }

        written.Should().Be(1);
        var events = await EventsAsync(legacy.Id);
        events.Single()["operation_type"].Should().Be(OutboxOperationTypes.Snapshot);
        (await EventsAsync(captured.Id)).Should().HaveCount(1, "an already captured patient is left alone");

        await using (var scope = _env.EdgeScope())
        {
            var writer = scope.ServiceProvider.GetRequiredService<IOutboxWriter>();
            (await scope.ServiceProvider.GetRequiredService<IPatientSyncService>().BackfillOutboxAsync(writer, 50, CancellationToken.None))
                .Should().Be(0, "running it again writes nothing");
        }
    }

    // ---- Monitoring -------------------------------------------------------------------------

    [Fact]
    public async Task Monitor_ListsPatientsAndHistory_WithoutPatientDetailsUnlessAllowed()
    {
        var patient = await _env.CreatePatientAsync("Monitor");

        await using var scope = _env.EdgeScope();
        var monitor = scope.ServiceProvider.GetRequiredService<ISyncMonitorService>();

        var summary = await monitor.GetSummaryAsync(CancellationToken.None);
        summary.Role.Should().Be("Edge");
        summary.HasCloudKey.Should().BeTrue();
        summary.PendingEvents.Should().BeGreaterThan(0);

        var hidden = await monitor.GetStreamsAsync(new SyncStreamListQuery { Search = patient.Id.ToString() }, includePatientDetails: false, CancellationToken.None);
        hidden.Items.Single().PatientName.Should().BeNull();

        var shown = await monitor.GetStreamsAsync(new SyncStreamListQuery { Search = patient.Uhid }, includePatientDetails: true, CancellationToken.None);
        shown.Items.Single().PatientName.Should().StartWith("Monitor");

        var history = await monitor.GetPatientHistoryAsync(patient.Id, includePatientDetails: false, CancellationToken.None);
        history.Single().OperationType.Should().Be("Insert");
    }

    private static async Task<string?> SnapshotAsync(IServiceProvider services, string connectionString, Guid patientId)
    {
        await using var connection = new Npgsql.NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        return await services.GetRequiredService<IPatientSyncService>().GetSnapshotJsonAsync(connection, null, patientId, CancellationToken.None);
    }
}
