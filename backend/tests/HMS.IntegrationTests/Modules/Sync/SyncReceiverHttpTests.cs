using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Sync;
using HMS.Modules.Sync.Contracts;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace HMS.IntegrationTests.Modules.Sync;

/// <summary>
/// The cloud side over real HTTP through the real HMS.Api pipeline (Sync:Role = Cloud): the
/// HmsSync key scheme, tenant resolution from the key, the receiver endpoint, and the
/// read-only patient guard after cutover. Databases as in <see cref="SyncTestEnvironment"/>
/// (HMS_TEST_POSTGRES, else Testcontainers); this host also migrates every module, so the server
/// needs the pgvector extension.
/// </summary>
public sealed class CloudApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string HospitalCode = "SYNCHTTP";
    public static readonly string SigningKey = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + "-integration-signing-key";

    private PostgreSqlContainer? _container;
    private string _admin = string.Empty;
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..8];

    public string TenantConnectionString { get; private set; } = string.Empty;
    private string PlatformConnectionString { get; set; } = string.Empty;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // UseSetting, not ConfigureAppConfiguration: Program.cs reads connection strings while
        // registering services, before app-configuration callbacks are applied. These values
        // must win, so the host can never touch a real development database.
        builder.UseSetting("ConnectionStrings:Platform", PlatformConnectionString);
        builder.UseSetting("ConnectionStrings:PlatformAdmin", _admin);
        builder.UseSetting("ConnectionStrings:Default", TenantConnectionString);
        builder.UseSetting("Bootstrap:SeedLegacyTenant", "true");
        builder.UseSetting("LegacyTenantSeed:HospitalCode", HospitalCode);
        builder.UseSetting("Jwt:SigningKey", SigningKey);
        builder.UseSetting("Sync:Role", "Cloud");
    }

    public async Task InitializeAsync()
    {
        _admin = Environment.GetEnvironmentVariable("HMS_TEST_POSTGRES") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_admin))
        {
            _container = new PostgreSqlBuilder().WithImage("pgvector/pgvector:pg16").WithDatabase("postgres").WithUsername("hms").WithPassword("hms").Build();
            await _container.StartAsync();
            _admin = _container.GetConnectionString();
        }

        PlatformConnectionString = await CreateDatabaseAsync($"hms_synchttp_{_suffix}_platform");
        TenantConnectionString = await CreateDatabaseAsync($"hms_synchttp_{_suffix}_tenant");
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        await using (var admin = new NpgsqlConnection(_admin))
        {
            await admin.OpenAsync();
            foreach (var name in new[] { $"hms_synchttp_{_suffix}_platform", $"hms_synchttp_{_suffix}_tenant" })
            {
                await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS {name} WITH (FORCE)", admin);
                await drop.ExecuteNonQueryAsync();
            }
        }

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    private async Task<string> CreateDatabaseAsync(string name)
    {
        await using var admin = new NpgsqlConnection(_admin);
        await admin.OpenAsync();
        await using var create = new NpgsqlCommand($"CREATE DATABASE {name}", admin);
        await create.ExecuteNonQueryAsync();
        return new NpgsqlConnectionStringBuilder(_admin) { Database = name }.ConnectionString;
    }
}

public class SyncReceiverHttpTests : IClassFixture<CloudApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly CloudApiFactory _factory;
    private readonly HttpClient _client;

    public SyncReceiverHttpTests(CloudApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> RunCommandAsync(params string[] args)
    {
        var output = new StringWriter();
        var exitCode = await SyncCommandLine.RunAsync(_factory.Services, args, output);
        exitCode.Should().Be(0, output.ToString());
        return output.ToString();
    }

    private async Task<string> IssueKeyAsync()
    {
        var output = await RunCommandAsync("sync-key", "issue", CloudApiFactory.HospitalCode);
        return output.Split('\n').Select(l => l.Trim()).Single(l => l.StartsWith("hmssync_", StringComparison.Ordinal));
    }

    private HttpRequestMessage SyncRequest(HttpMethod method, string path, string key, object? body = null)
    {
        var request = new HttpRequestMessage(method, path) { Content = body is null ? null : JsonContent.Create(body, options: Json) };
        request.Headers.Authorization = new AuthenticationHeaderValue("HmsSync", key);
        return request;
    }

    private async Task<SyncBatchRequest> BatchForNewPatientAsync()
    {
        Guid stateId, districtId;
        await using (var connection = new NpgsqlConnection(_factory.TenantConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("SELECT state_id, id FROM masters.districts ORDER BY id LIMIT 1", connection);
            await using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            stateId = reader.GetGuid(0);
            districtId = reader.GetGuid(1);
        }

        var id = Guid.CreateVersion7();
        var snapshot = new PatientSyncSnapshot
        {
            Id = id,
            Uhid = "E-" + id.ToString("N")[..10],
            Title = "Ms",
            FirstName = "Http",
            LastName = "Replica",
            DateOfBirth = new DateOnly(1990, 1, 2),
            Gender = "Female",
            BloodGroup = "APositive",
            MaritalStatus = "Unmarried",
            PrimaryPhone = "9123456780",
            ModeOfArrivalSource = "DoctorReferral",
            CreatedAt = DateTime.UtcNow,
            Address = new PatientSyncAddress { AddressLine1 = "1 Cloud Road", StateId = stateId, DistrictId = districtId, Pincode = "600001" },
            EmergencyContacts = [new PatientSyncEmergencyContact { Id = Guid.CreateVersion7(), Relationship = "Mother", Name = "Mum", Phone = "9123456781" }],
        };
        var payload = JsonSerializer.Serialize(snapshot, Json);

        string hash;
        await using (var connection = new NpgsqlConnection(_factory.TenantConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("SELECT encode(sha256(convert_to(CAST(@p AS jsonb)::text, 'UTF8')), 'hex')", connection);
            command.Parameters.AddWithValue("p", payload);
            hash = (string)(await command.ExecuteScalarAsync())!;
        }

        using var document = JsonDocument.Parse(payload);
        return new SyncBatchRequest
        {
            SourceNodeId = "http-test",
            SourceEpoch = Guid.NewGuid(),
            Events =
            [
                new SyncEventEnvelope
                {
                    EventId = Guid.CreateVersion7(),
                    StreamId = id,
                    AggregateType = PatientSyncAggregate.AggregateType,
                    AggregateId = id,
                    OperationType = "Insert",
                    SequenceNumber = 1,
                    PayloadSchemaVersion = 1,
                    PayloadHash = hash,
                    OccurredAt = DateTime.UtcNow,
                    Payload = document.RootElement.Clone(),
                },
            ],
        };
    }

    [Fact]
    public void SyncEndpoints_AreRegisteredWithTheHost()
    {
        // Guards against the module's controllers silently missing from MVC's application parts.
        var routes = _factory.Services.GetServices<Microsoft.AspNetCore.Routing.EndpointDataSource>()
            .SelectMany(s => s.Endpoints)
            .OfType<Microsoft.AspNetCore.Routing.RouteEndpoint>()
            .Select(e => e.RoutePattern.RawText)
            .ToList();

        routes.Should().Contain(["api/v1/sync/ping", "api/v1/sync/patients", "api/v1/sync-monitor/summary", "api/v1/sync-monitor/events/{eventId:guid}/resolve"]);
    }

    [Fact]
    public async Task ValidKey_AppliesABatch_AndAResendIsADuplicate()
    {
        var key = await IssueKeyAsync();
        var batch = await BatchForNewPatientAsync();

        var ping = await _client.SendAsync(SyncRequest(HttpMethod.Get, "/api/v1/sync/ping", key));
        ping.StatusCode.Should().Be(HttpStatusCode.OK, await ping.Content.ReadAsStringAsync());
        (await ping.Content.ReadFromJsonAsync<ApiResponse<SyncPingResponse>>(Json))!.Data!.HospitalCode.Should().BeEquivalentTo(CloudApiFactory.HospitalCode);

        var first = await _client.SendAsync(SyncRequest(HttpMethod.Post, "/api/v1/sync/patients", key, batch));
        first.StatusCode.Should().Be(HttpStatusCode.OK, await first.Content.ReadAsStringAsync());
        var applied = (await first.Content.ReadFromJsonAsync<ApiResponse<SyncBatchResponse>>(Json))!.Data!.Results.Single();
        applied.Status.Should().Be(SyncResultStatuses.Applied);
        applied.ReceiptId.Should().NotBeNull();

        var second = await _client.SendAsync(SyncRequest(HttpMethod.Post, "/api/v1/sync/patients", key, batch));
        var duplicate = (await second.Content.ReadFromJsonAsync<ApiResponse<SyncBatchResponse>>(Json))!.Data!.Results.Single();
        duplicate.Status.Should().Be(SyncResultStatuses.Duplicate);
        duplicate.ReceiptId.Should().Be(applied.ReceiptId, "the original acknowledgement is returned");
    }

    [Fact]
    public async Task MissingWrongOrRevokedKey_IsRejected()
    {
        var batch = await BatchForNewPatientAsync();

        (await _client.PostAsJsonAsync("/api/v1/sync/patients", batch, Json)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await _client.SendAsync(SyncRequest(HttpMethod.Post, "/api/v1/sync/patients", "hmssync_" + new string('A', 43), batch)))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var key = await IssueKeyAsync();
        await RunCommandAsync("sync-key", "revoke", CloudApiFactory.HospitalCode, key[..14]);
        (await _client.SendAsync(SyncRequest(HttpMethod.Get, "/api/v1/sync/ping", key))).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HospitalUserToken_CannotCallTheReceiver_AndASyncKeyCannotCallUserEndpoints()
    {
        var token = await HospitalTokenAsync("patient-management.view", "patient-management.create");
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/sync/ping");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        (await _client.SendAsync(request)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var key = await IssueKeyAsync();
        (await _client.SendAsync(SyncRequest(HttpMethod.Get, "/api/v1/patients", key))).StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AfterCutover_CloudRefusesPatientWrites_ButStillReadsThem()
    {
        var token = await HospitalTokenAsync("patient-management.view", "patient-management.create");
        await RunCommandAsync("sync-cutover", CloudApiFactory.HospitalCode, "on");

        try
        {
            var create = new HttpRequestMessage(HttpMethod.Post, "/api/v1/patients") { Content = JsonContent.Create(new { firstName = "x" }) };
            create.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _client.SendAsync(create);
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await response.Content.ReadFromJsonAsync<ApiErrorResponse>(Json))!.ErrorCode.Should().Be("PATIENTS.MANAGED_BY_LOCAL_SERVER");

            var list = new HttpRequestMessage(HttpMethod.Get, "/api/v1/patients");
            list.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            (await _client.SendAsync(list)).StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await RunCommandAsync("sync-cutover", CloudApiFactory.HospitalCode, "off");
        }
    }

    // A token shaped like JwtTokenGenerator's, signed with this test host's key; no login involved.
    private async Task<string> HospitalTokenAsync(params string[] permissions)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var tenant = await scope.ServiceProvider.GetRequiredService<ITenantDirectory>().FindByHospitalCodeAsync(CloudApiFactory.HospitalCode, CancellationToken.None);
        var claims = new List<Claim>
        {
            new("UserId", Guid.NewGuid().ToString()),
            new("Username", "sync-test"),
            new("RoleId", Guid.NewGuid().ToString()),
            new("RoleName", "Tester"),
            new("LoginType", "hospital"),
            new("TenantId", tenant!.Id.ToString()),
        };
        claims.AddRange(permissions.Select(p => new Claim("Permission", p)));

        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(CloudApiFactory.SigningKey)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken("HMS", "HMS.Clients", claims, expires: DateTime.UtcNow.AddMinutes(10), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
