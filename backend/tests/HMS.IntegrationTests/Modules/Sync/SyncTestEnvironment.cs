using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using HMS.Modules.ActivityLog;
using HMS.Modules.ActivityLog.Infrastructure;
using HMS.Modules.Masters;
using HMS.Modules.Masters.Infrastructure;
using HMS.Modules.Patients;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Infrastructure;
using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Sync;
using HMS.Modules.Sync.Application;
using HMS.Modules.Sync.Contracts;
using HMS.Modules.Sync.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace HMS.IntegrationTests.Modules.Sync;

/// <summary>
/// Two real hospital databases, one playing the local (Edge) server and one the cloud, each
/// migrated with the Masters, Patients, ActivityLog and Sync schemas, plus a DI container per
/// role composed from the real modules. The Edge's cloud HTTP client is wired to an in-process
/// handler that runs the real cloud receiver against the cloud database, with switchable faults
/// (offline, timeout, 500, 401).
///
/// Database server: the <c>HMS_TEST_POSTGRES</c> environment variable (an admin connection string to
/// an existing PostgreSQL 13+, e.g. a local development server) when set; otherwise a disposable
/// Testcontainers PostgreSQL, which needs Docker. The databases are created with random names and
/// dropped afterwards.
/// </summary>
public sealed class SyncTestEnvironment : IAsyncLifetime
{
    public const string HospitalCode = "SYNCT";
    public const string ApiKey = "hmssync_integrationtest_key_value_000000000000";

    public static readonly Guid EdgeTenantId = Guid.NewGuid();
    public static readonly Guid CloudTenantId = Guid.NewGuid();

    private PostgreSqlContainer? _container;
    private string _adminConnectionString = string.Empty;
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..8];

    public string EdgeConnectionString { get; private set; } = string.Empty;
    public string CloudConnectionString { get; private set; } = string.Empty;

    public ServiceProvider Edge { get; private set; } = null!;
    public ServiceProvider Cloud { get; private set; } = null!;
    public FakeCloudHandler CloudHandler { get; private set; } = null!;

    public Guid StateId { get; private set; }
    public Guid DistrictId { get; private set; }

    /// <summary>A second district of the same state, deleted on the cloud only (for permanent
    /// rejection scenarios).</summary>
    public Guid EdgeOnlyDistrictId { get; private set; }

    public async Task InitializeAsync()
    {
        _adminConnectionString = Environment.GetEnvironmentVariable("HMS_TEST_POSTGRES") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_adminConnectionString))
        {
            _container = new PostgreSqlBuilder().WithImage("postgres:16").WithDatabase("postgres").WithUsername("hms").WithPassword("hms").Build();
            await _container.StartAsync();
            _adminConnectionString = _container.GetConnectionString();
        }

        EdgeConnectionString = await CreateDatabaseAsync($"hms_synctest_{_suffix}_edge");
        CloudConnectionString = await CreateDatabaseAsync($"hms_synctest_{_suffix}_cloud");

        await MigrateAsync(EdgeConnectionString);
        await MigrateAsync(CloudConnectionString);

        await using (var edge = await OpenAsync(EdgeConnectionString))
        {
            await using var command = new NpgsqlCommand(
                """
                SELECT d1.state_id, d1.id, d2.id
                  FROM masters.districts d1
                  JOIN masters.districts d2 ON d2.state_id = d1.state_id AND d2.id <> d1.id
                 ORDER BY d1.state_id, d1.id, d2.id
                 LIMIT 1
                """,
                edge);
            await using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            StateId = reader.GetGuid(0);
            DistrictId = reader.GetGuid(1);
            EdgeOnlyDistrictId = reader.GetGuid(2);
        }

        await ExecuteAsync(CloudConnectionString, $"DELETE FROM masters.districts WHERE id = '{EdgeOnlyDistrictId}'");

        CloudHandler = new FakeCloudHandler(this);
        Cloud = BuildProvider(SyncRole.Cloud, CloudConnectionString, CloudTenantId, configure: null);
        Edge = BuildProvider(SyncRole.Edge, EdgeConnectionString, EdgeTenantId, services =>
            services.AddHttpClient(CloudSyncClient.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => CloudHandler));
    }

    public async Task DisposeAsync()
    {
        await Edge.DisposeAsync();
        await Cloud.DisposeAsync();
        NpgsqlConnection.ClearAllPools();

        await using (var admin = await OpenAsync(_adminConnectionString))
        {
            foreach (var name in new[] { $"hms_synctest_{_suffix}_edge", $"hms_synctest_{_suffix}_cloud" })
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

    /// <summary>A DI scope bound to the Edge hospital, like a request or a sync job would have.</summary>
    public AsyncServiceScope EdgeScope() => Scope(Edge, EdgeTenantId, EdgeConnectionString);

    public AsyncServiceScope CloudScope() => Scope(Cloud, CloudTenantId, CloudConnectionString);

    public TenantInfo EdgeTenant => new(EdgeTenantId, HospitalCode, "edge", EdgeConnectionString, true, [], []);

    /// <summary>One Edge sync run, exactly as SyncTenantJob performs it.</summary>
    public async Task<(string Outcome, int Sent, int Synced)> RunSyncAsync()
    {
        await using var scope = EdgeScope();
        var summary = await scope.ServiceProvider.GetRequiredService<SyncTenantProcessor>().RunAsync(EdgeTenant, CancellationToken.None);
        return (summary.Outcome, summary.EventsSent, summary.EventsSynced);
    }

    /// <summary>Clears offline/auth/pause flags and back-offs left by an earlier test.</summary>
    public async Task ResetEdgeStateAsync()
    {
        CloudHandler.Mode = CloudMode.Online;
        await ExecuteAsync(EdgeConnectionString, """
            INSERT INTO sync.sync_state (id, updated_at) VALUES (1, now()) ON CONFLICT (id) DO NOTHING;
            UPDATE sync.sync_state SET cloud_reachable = true, auth_failed = false, is_paused = false;
            UPDATE sync.outbox_streams SET next_attempt_at = NULL WHERE status IN ('Pending', 'Failed');
            """);
    }

    public CreatePatientRequest NewPatientRequest(string firstName)
    {
        var phone = "9" + Random.Shared.NextInt64(100_000_000, 999_999_999).ToString();
        return new CreatePatientRequest
        {
            Title = Title.Mr,
            FirstName = firstName,
            LastName = "Sync" + Guid.NewGuid().ToString("N")[..6],
            DateOfBirth = new DateOnly(1985, 3, 14),
            Gender = Gender.Male,
            BloodGroup = BloodGroup.BPositive,
            MaritalStatus = MaritalStatus.Married,
            PrimaryPhone = phone,
            ModeOfArrivalSource = ModeOfArrivalSource.DoctorReferral,
            Address = new AddressRequest { AddressLine1 = "1 Test Street", StateId = StateId, DistrictId = DistrictId, Pincode = "627001" },
            Allergies = [new AllergyRequest { AllergyType = AllergyType.Drug, Specify = "Penicillin", Severity = AllergySeverity.Mild }],
            EmergencyContacts = [new EmergencyContactRequest { Relationship = Relationship.Spouse, Name = "Contact", Phone = "9000000001" }],
        };
    }

    public static UpdatePatientRequest ToUpdate(PatientResponse p, Func<UpdatePatientRequest, UpdatePatientRequest>? change = null)
    {
        var request = new UpdatePatientRequest
        {
            Title = p.Title,
            FirstName = p.FirstName,
            LastName = p.LastName,
            DateOfBirth = p.DateOfBirth,
            Gender = p.Gender,
            BloodGroup = p.BloodGroup,
            MaritalStatus = p.MaritalStatus,
            PrimaryPhone = p.PrimaryPhone,
            SecondaryPhone = p.SecondaryPhone,
            Email = p.Email,
            Profession = p.Profession,
            IdProofType = p.IdProofType,
            IdProofNumber = p.IdProofNumber,
            ModeOfArrivalSource = p.ModeOfArrivalSource,
            ModeOfArrivalChannel = p.ModeOfArrivalChannel,
            ModeOfArrivalSpecify = p.ModeOfArrivalSpecify,
            Address = new AddressRequest
            {
                AddressLine1 = p.Address.AddressLine1,
                AddressLine2 = p.Address.AddressLine2,
                AddressLine3 = p.Address.AddressLine3,
                StateId = p.Address.StateId,
                DistrictId = p.Address.DistrictId,
                Pincode = p.Address.Pincode,
            },
            RowVersion = p.RowVersion,
        };

        return change is null ? request : change(request);
    }

    public async Task<PatientResponse> CreatePatientAsync(string firstName)
    {
        await using var scope = EdgeScope();
        var result = await scope.ServiceProvider.GetRequiredService<IPatientService>().CreateAsync(NewPatientRequest(firstName), Guid.NewGuid(), CancellationToken.None);
        result.IsSuccess.Should().BeTrue(result.Error);
        return result.Value!;
    }

    public async Task<PatientResponse> UpdatePatientAsync(Guid patientId, Func<UpdatePatientRequest, UpdatePatientRequest> change)
    {
        await using var scope = EdgeScope();
        var patients = scope.ServiceProvider.GetRequiredService<IPatientService>();
        var current = (await patients.GetByIdAsync(patientId, CancellationToken.None)).Value!;
        var result = await patients.UpdateAsync(patientId, ToUpdate(current, change), Guid.NewGuid(), CancellationToken.None);
        result.IsSuccess.Should().BeTrue(result.Error);
        return result.Value!;
    }

    public static async Task<T?> ScalarAsync<T>(string connectionString, string sql)
    {
        await using var connection = await OpenAsync(connectionString);
        await using var command = new NpgsqlCommand(sql, connection);
        var value = await command.ExecuteScalarAsync();
        return value is null or DBNull ? default : (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
    }

    public static async Task<List<Dictionary<string, object?>>> RowsAsync(string connectionString, string sql)
    {
        await using var connection = await OpenAsync(connectionString);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            rows.Add(row);
        }

        return rows;
    }

    public static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = await OpenAsync(connectionString);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<NpgsqlConnection> OpenAsync(string connectionString)
    {
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }

    private async Task<string> CreateDatabaseAsync(string name)
    {
        await using (var admin = await OpenAsync(_adminConnectionString))
        {
            await using var create = new NpgsqlCommand($"CREATE DATABASE {name}", admin);
            await create.ExecuteNonQueryAsync();
        }

        return new NpgsqlConnectionStringBuilder(_adminConnectionString) { Database = name, Pooling = true }.ConnectionString;
    }

    private static async Task MigrateAsync(string connectionString)
    {
        await using (var db = new MastersDbContext(Options<MastersDbContext>(connectionString, MastersDbContext.SchemaName)))
        {
            await db.Database.MigrateAsync();
        }

        await using (var db = new PatientsDbContext(Options<PatientsDbContext>(connectionString, PatientsDbContext.SchemaName)))
        {
            await db.Database.MigrateAsync();
        }

        await using (var db = new ActivityLogDbContext(Options<ActivityLogDbContext>(connectionString, ActivityLogDbContext.SchemaName)))
        {
            await db.Database.MigrateAsync();
        }

        await using (var db = new SyncDbContext(Options<SyncDbContext>(connectionString, SyncDbContext.SchemaName)))
        {
            await db.Database.MigrateAsync();
        }
    }

    private static DbContextOptions<T> Options<T>(string connectionString, string schema)
        where T : DbContext =>
        new DbContextOptionsBuilder<T>()
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", schema);
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            })
            .Options;

    private static AsyncServiceScope Scope(ServiceProvider provider, Guid tenantId, string connectionString)
    {
        var scope = provider.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<ITenantContext>().SetTenant(tenantId, connectionString);
        return scope;
    }

    private ServiceProvider BuildProvider(SyncRole role, string connectionString, Guid tenantId, Action<IServiceCollection>? configure)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Only the registry context reads it, and these tests never resolve that.
                ["ConnectionStrings:Platform"] = "Host=127.0.0.1;Port=1;Database=unused",
                ["Sync:Role"] = role.ToString(),
                ["Sync:NodeId"] = "integration-" + role,
                ["Sync:Cloud:BaseUrl"] = "http://localhost:59999",
                [$"Sync:Cloud:ApiKeys:{HospitalCode}"] = ApiKey,
                ["Sync:MaxRunSeconds"] = "20",
                ["Sync:HttpTimeoutInSeconds"] = "2",
                ["Sync:LeaseSeconds"] = "60",
                ["Sync:ConnectivityBackoffSeconds"] = "60",
                ["Sync:BatchSize"] = "50",
                ["Sync:MaxEventsPerStream"] = "10",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddActivityLogModule(configuration);
        services.AddMastersModule(configuration);
        services.AddPatientsModule(configuration);
        services.AddSyncModule(configuration);
        services.AddSingleton<ITenantDirectory>(new SingleTenantDirectory(new TenantInfo(tenantId, HospitalCode, "db", connectionString, true, [], [])));
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    private sealed class SingleTenantDirectory(TenantInfo tenant) : ITenantDirectory
    {
        public Task<TenantInfo?> FindByIdAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult<TenantInfo?>(tenantId == tenant.Id ? tenant : null);

        public Task<TenantInfo?> FindByHospitalCodeAsync(string hospitalCode, CancellationToken cancellationToken) =>
            Task.FromResult<TenantInfo?>(string.Equals(hospitalCode, tenant.HospitalCode, StringComparison.OrdinalIgnoreCase) ? tenant : null);

        public Task<IReadOnlyList<TenantInfo>> GetAllActiveTenantsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TenantInfo>>([tenant]);
    }
}

public enum CloudMode
{
    Online,
    Offline,
    Timeout,
    ServerError,
    Unauthorized,
}

/// <summary>Stands in for the network between the two servers. Online, it deserialises the batch,
/// runs the real <see cref="ISyncReceiverService"/> against the cloud database and serialises the
/// answer, so the payload and hash travel exactly as they would over HTTP.</summary>
public sealed class FakeCloudHandler(SyncTestEnvironment environment) : HttpMessageHandler
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public CloudMode Mode { get; set; } = CloudMode.Online;

    public int BatchRequests { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        switch (Mode)
        {
            case CloudMode.Offline:
                throw new HttpRequestException(HttpRequestError.NameResolutionError, "No such host is known.");
            case CloudMode.Timeout:
                await Task.Delay(Timeout.Infinite, cancellationToken);
                break;
            case CloudMode.ServerError:
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            case CloudMode.Unauthorized:
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        }

        if (request.Headers.Authorization is not { Scheme: SyncAuthenticationDefaults.Scheme } auth || auth.Parameter != SyncTestEnvironment.ApiKey)
        {
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        }

        var path = request.RequestUri!.AbsolutePath;
        if (path.EndsWith("/sync/ping", StringComparison.Ordinal))
        {
            return JsonResponse(new ApiResponse<SyncPingResponse> { Data = new SyncPingResponse { HospitalCode = SyncTestEnvironment.HospitalCode, ServerTime = DateTime.UtcNow } });
        }

        BatchRequests++;
        var batch = await request.Content!.ReadFromJsonAsync<SyncBatchRequest>(Json, cancellationToken);
        await using var scope = environment.CloudScope();
        var response = await scope.ServiceProvider.GetRequiredService<ISyncReceiverService>().ProcessAsync(batch!, cancellationToken);
        return JsonResponse(new ApiResponse<SyncBatchResponse> { Data = response });
    }

    private static HttpResponseMessage JsonResponse<T>(T body) => new(HttpStatusCode.OK) { Content = JsonContent.Create(body, options: Json) };
}
