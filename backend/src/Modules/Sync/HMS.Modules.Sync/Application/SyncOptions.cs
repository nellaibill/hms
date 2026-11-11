using HMS.Modules.Sync.Contracts;
using Microsoft.Extensions.Options;

namespace HMS.Modules.Sync.Application;

/// <summary>
/// Bound from the <c>Sync</c> configuration section (ADR-133). Holds no secrets except
/// <see cref="SyncCloudOptions.ApiKeys"/>, which must only ever come from environment
/// variables (<c>Sync__Cloud__ApiKeys__LHS=...</c>) or another secrets provider, never from a
/// committed appsettings file.
/// </summary>
internal sealed class SyncOptions
{
    public const string SectionName = "Sync";

    public static readonly int[] DefaultRetryScheduleMinutes = [1, 2, 5, 10, 30];

    /// <summary>None (default) | Edge | Cloud.</summary>
    public SyncRole Role { get; set; } = SyncRole.None;

    /// <summary>Edge only: master switch for sending. Capture into the outbox continues while
    /// this is false, so nothing is lost; sending resumes when it is turned back on.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How often the dispatcher runs (minutes, 1-59).</summary>
    public int IntervalInMinutes { get; set; } = 1;

    /// <summary>Maximum events in one HTTP batch.</summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>Maximum events of one patient in one HTTP batch.</summary>
    public int MaxEventsPerStream { get; set; } = 20;

    /// <summary>One tenant run keeps sending batches until the backlog is empty or this many
    /// seconds have passed.</summary>
    public int MaxRunSeconds { get; set; } = 50;

    /// <summary>Automatic retries of a transiently failing event before it needs attention.</summary>
    public int MaxRetryCount { get; set; } = 5;

    /// <summary>Delay before retry 1, 2, 3 ... (minutes). The last value repeats. Null means
    /// <see cref="DefaultRetryScheduleMinutes"/> (an array default would be appended to, not
    /// replaced, by the configuration binder).</summary>
    public int[]? RetryScheduleMinutes { get; set; }

    public int HttpTimeoutInSeconds { get; set; } = 30;

    /// <summary>How long a worker's claim on a patient stream lasts. Must comfortably exceed
    /// one HTTP call, so an in-flight batch is never claimed twice.</summary>
    public int LeaseSeconds { get; set; } = 120;

    /// <summary>Back-off for a stream after a connectivity failure (no retry is counted).</summary>
    public int ConnectivityBackoffSeconds { get; set; } = 60;

    /// <summary>Synced events (and their attempts) older than this are purged daily.</summary>
    public int RetentionDays { get; set; } = 30;

    /// <summary>Identifies this server in cloud-side logs. Defaults to the machine name.</summary>
    public string? NodeId { get; set; }

    public SyncCloudOptions Cloud { get; set; } = new();

    public IReadOnlyList<int> EffectiveRetrySchedule =>
        RetryScheduleMinutes is { Length: > 0 } schedule ? schedule : DefaultRetryScheduleMinutes;

    public string EffectiveNodeId => string.IsNullOrWhiteSpace(NodeId) ? Environment.MachineName : NodeId!;

    public bool IsEdge => Role == SyncRole.Edge;

    public bool IsCloud => Role == SyncRole.Cloud;

    /// <summary>The key this Edge server presents for a hospital, or null if none is configured.
    /// Case-insensitive whatever dictionary the configuration binder produced: hospital codes are
    /// stored lowercase, while operators usually write the variable as Sync__Cloud__ApiKeys__LHS.</summary>
    public string? FindApiKey(string hospitalCode)
    {
        var key = Cloud.ApiKeys.FirstOrDefault(k => string.Equals(k.Key, hospitalCode, StringComparison.OrdinalIgnoreCase)).Value;
        return string.IsNullOrWhiteSpace(key) ? null : key.Trim();
    }
}

internal sealed class SyncCloudOptions
{
    /// <summary>Base URL of the cloud HMS API, e.g. https://cloud.example.com. Must be https
    /// (plain http is accepted only for localhost, for development).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>HospitalCode → sync API key, issued on the cloud with
    /// <c>dotnet HMS.Api.dll sync-key issue &lt;hospitalCode&gt;</c>. Environment variables only.</summary>
    public Dictionary<string, string> ApiKeys { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Bound from the top-level <c>Hangfire</c> section. Used on an Edge server only.</summary>
internal sealed class SyncHangfireOptions
{
    public const string SectionName = "Hangfire";

    public int WorkerCount { get; set; } = 2;
    public string SchemaName { get; set; } = "hangfire";
    public string DashboardPath { get; set; } = "/hangfire";
}

/// <summary>Fails startup (ValidateOnStart) on a misconfigured server instead of letting it run
/// and silently never sync.</summary>
internal sealed class SyncOptionsValidator : IValidateOptions<SyncOptions>
{
    public ValidateOptionsResult Validate(string? name, SyncOptions options)
    {
        var errors = new List<string>();

        if (!Enum.IsDefined(options.Role))
        {
            errors.Add("Sync:Role must be None, Edge or Cloud.");
        }

        if (options.IntervalInMinutes is < 1 or > 59)
        {
            errors.Add("Sync:IntervalInMinutes must be between 1 and 59.");
        }

        if (options.BatchSize is < 1 or > 1000)
        {
            errors.Add("Sync:BatchSize must be between 1 and 1000.");
        }

        if (options.MaxEventsPerStream < 1 || options.MaxEventsPerStream > options.BatchSize)
        {
            errors.Add("Sync:MaxEventsPerStream must be between 1 and Sync:BatchSize.");
        }

        if (options.MaxRunSeconds is < 5 or > 3600)
        {
            errors.Add("Sync:MaxRunSeconds must be between 5 and 3600.");
        }

        if (options.MaxRetryCount is < 0 or > 100)
        {
            errors.Add("Sync:MaxRetryCount must be between 0 and 100.");
        }

        if (options.RetryScheduleMinutes is { } schedule && schedule.Any(m => m < 1 || m > 1440))
        {
            errors.Add("Sync:RetryScheduleMinutes values must be between 1 and 1440.");
        }

        if (options.HttpTimeoutInSeconds is < 1 or > 300)
        {
            errors.Add("Sync:HttpTimeoutInSeconds must be between 1 and 300.");
        }

        if (options.LeaseSeconds < options.HttpTimeoutInSeconds + 30)
        {
            errors.Add("Sync:LeaseSeconds must be at least Sync:HttpTimeoutInSeconds + 30, so an in-flight batch is never claimed twice.");
        }

        if (options.ConnectivityBackoffSeconds is < 5 or > 3600)
        {
            errors.Add("Sync:ConnectivityBackoffSeconds must be between 5 and 3600.");
        }

        if (options.RetentionDays is < 1 or > 3650)
        {
            errors.Add("Sync:RetentionDays must be between 1 and 3650.");
        }

        if (options.Role == SyncRole.Edge)
        {
            ValidateCloudBaseUrl(options.Cloud.BaseUrl, errors);
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }

    private static void ValidateCloudBaseUrl(string baseUrl, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            errors.Add("Sync:Cloud:BaseUrl must be an absolute URL when Sync:Role is Edge.");
            return;
        }

        var isHttps = uri.Scheme == Uri.UriSchemeHttps;
        var isLocalHttp = uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback;
        if (!isHttps && !isLocalHttp)
        {
            errors.Add("Sync:Cloud:BaseUrl must use https (plain http is allowed only for localhost).");
        }
    }
}
