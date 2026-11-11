using HMS.Modules.Patients.Application;
using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Sync.Application;
using HMS.Modules.Sync.Domain;
using HMS.Modules.Sync.Infrastructure;
using HMS.Shared.Kernel;
using HMS.Shared.Kernel.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HMS.Modules.Sync;

/// <summary>
/// Server-side operator commands for the offline-first sync (ADR-133), run like the existing
/// <c>migrate</c> command and never exposed over HTTP:
/// <code>
/// dotnet HMS.Api.dll sync-key issue    &lt;hospitalCode&gt;           (Cloud) prints a new key ONCE
/// dotnet HMS.Api.dll sync-key list     &lt;hospitalCode&gt;           (Cloud)
/// dotnet HMS.Api.dll sync-key revoke   &lt;hospitalCode&gt; [prefix]  (Cloud) all keys, or the one with that prefix
/// dotnet HMS.Api.dll sync-cutover      &lt;hospitalCode&gt; on|off    (Cloud) patients become read-only in the cloud
/// dotnet HMS.Api.dll sync-backfill     &lt;hospitalCode&gt;           (Edge)  queue every patient that has never been synced
/// </code>
/// </summary>
public static class SyncCommandLine
{
    private static readonly string[] Commands = ["sync-key", "sync-cutover", "sync-backfill"];

    /// <summary>True when <paramref name="args"/> asks for one of the sync commands.</summary>
    public static bool IsSyncCommand(string[] args) => FindCommandIndex(args) >= 0;

    /// <summary>Runs the command and returns the process exit code.</summary>
    public static async Task<int> RunAsync(IServiceProvider services, string[] args, TextWriter output)
    {
        var index = FindCommandIndex(args);
        var command = args[index].ToLowerInvariant();
        var rest = PositionalArguments(args, index);

        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var options = sp.GetRequiredService<IOptions<SyncOptions>>().Value;

        try
        {
            return command switch
            {
                "sync-key" => await RunKeyCommandAsync(sp, options, rest, output),
                "sync-cutover" => await RunCutoverAsync(sp, options, rest, output),
                "sync-backfill" => await RunBackfillAsync(sp, options, rest, output),
                _ => Usage(output),
            };
        }
        catch (CommandLineException exception)
        {
            await output.WriteLineAsync("Error: " + exception.Message);
            return 1;
        }
    }

    private static async Task<int> RunKeyCommandAsync(IServiceProvider sp, SyncOptions options, string[] rest, TextWriter output)
    {
        if (rest.Length < 2)
        {
            return Usage(output);
        }

        RequireRole(options.IsCloud, "sync-key runs on the Cloud server (Sync:Role = Cloud).");
        var tenant = await FindTenantAsync(sp, rest[1]);
        var registry = sp.GetRequiredService<SyncRegistryDbContext>();

        switch (rest[0].ToLowerInvariant())
        {
            case "issue":
            {
                var key = SyncApiKeys.Generate();
                registry.Clients.Add(new SyncClient
                {
                    Id = Guid.CreateVersion7(),
                    TenantId = tenant.Id,
                    HospitalCode = tenant.HospitalCode,
                    KeyPrefix = SyncApiKeys.DisplayPrefix(key),
                    KeyHash = SyncApiKeys.Hash(key),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = Environment.UserName,
                });
                await registry.SaveChangesAsync();

                await output.WriteLineAsync($"New sync key for hospital {tenant.HospitalCode} (shown once, store it now):");
                await output.WriteLineAsync();
                await output.WriteLineAsync(key);
                await output.WriteLineAsync();
                await output.WriteLineAsync($"On the local server set the environment variable  Sync__Cloud__ApiKeys__{tenant.HospitalCode}  to this value.");
                return 0;
            }

            case "list":
            {
                var keys = await registry.Clients.AsNoTracking().Where(c => c.TenantId == tenant.Id).OrderBy(c => c.CreatedAt).ToListAsync();
                await output.WriteLineAsync($"{keys.Count} sync key(s) for hospital {tenant.HospitalCode}:");
                foreach (var key in keys)
                {
                    var state = key.RevokedAt is null ? "active" : $"revoked {key.RevokedAt:u}";
                    await output.WriteLineAsync($"  {key.KeyPrefix}…  created {key.CreatedAt:u} by {key.CreatedBy ?? "?"}, last used {(key.LastUsedAt?.ToString("u") ?? "never")}, {state}");
                }

                return 0;
            }

            case "revoke":
            {
                var prefix = rest.Length > 2 ? rest[2] : null;
                var now = DateTime.UtcNow;
                var revoked = await registry.Clients
                    .Where(c => c.TenantId == tenant.Id && c.RevokedAt == null && (prefix == null || c.KeyPrefix == prefix))
                    .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.RevokedAt, now));
                await output.WriteLineAsync($"Revoked {revoked} key(s) for hospital {tenant.HospitalCode}.");
                return 0;
            }

            default:
                return Usage(output);
        }
    }

    private static async Task<int> RunCutoverAsync(IServiceProvider sp, SyncOptions options, string[] rest, TextWriter output)
    {
        if (rest.Length < 2 || rest[1].ToLowerInvariant() is not ("on" or "off"))
        {
            return Usage(output);
        }

        RequireRole(options.IsCloud, "sync-cutover runs on the Cloud server (Sync:Role = Cloud).");
        var tenant = await FindTenantAsync(sp, rest[0]);
        var on = rest[1].Equals("on", StringComparison.OrdinalIgnoreCase);
        var registry = sp.GetRequiredService<SyncRegistryDbContext>();

        var settings = await registry.TenantSettings.FirstOrDefaultAsync(s => s.TenantId == tenant.Id);
        if (settings is null)
        {
            settings = new TenantSyncSettings { TenantId = tenant.Id, HospitalCode = tenant.HospitalCode };
            registry.TenantSettings.Add(settings);
        }

        settings.PatientsOwnedByEdge = on;
        settings.UpdatedAt = DateTime.UtcNow;
        settings.UpdatedBy = Environment.UserName;
        await registry.SaveChangesAsync();

        await output.WriteLineAsync(on
            ? $"Hospital {tenant.HospitalCode}: patients are now managed by the local server. Registering or editing patients in the cloud is refused."
            : $"Hospital {tenant.HospitalCode}: patients can be registered and edited in the cloud again.");
        return 0;
    }

    private static async Task<int> RunBackfillAsync(IServiceProvider sp, SyncOptions options, string[] rest, TextWriter output)
    {
        if (rest.Length < 1)
        {
            return Usage(output);
        }

        RequireRole(options.IsEdge, "sync-backfill runs on the local (Edge) server (Sync:Role = Edge).");
        var tenant = await FindTenantAsync(sp, rest[0]);
        sp.GetRequiredService<ITenantContext>().SetTenant(tenant.Id, tenant.ConnectionString, tenant.EnabledModules, tenant.EnabledFeatures);

        var writer = sp.GetRequiredService<IOutboxWriter>();
        var written = await sp.GetRequiredService<IPatientSyncService>().BackfillOutboxAsync(writer, 500, CancellationToken.None);
        await output.WriteLineAsync($"Hospital {tenant.HospitalCode}: queued {written} patient(s) that had never been synced. They are sent on the next sync run.");
        return 0;
    }

    private static async Task<TenantInfo> FindTenantAsync(IServiceProvider sp, string hospitalCode)
    {
        var tenant = await sp.GetRequiredService<ITenantDirectory>().FindByHospitalCodeAsync(hospitalCode, CancellationToken.None);
        return tenant is { IsActive: true }
            ? tenant
            : throw new CommandLineException($"No active hospital with code '{hospitalCode}'.");
    }

    private static void RequireRole(bool ok, string message)
    {
        if (!ok)
        {
            throw new CommandLineException(message);
        }
    }

    /// <summary>The command's own arguments: the tokens right after it, up to the first switch.
    /// A switch's value (as in <c>--environment Production</c>) is never mistaken for one.</summary>
    internal static string[] PositionalArguments(string[] args, int commandIndex) =>
        args.Skip(commandIndex + 1).TakeWhile(a => !a.StartsWith('-') && !a.Contains('=')).ToArray();

    private static int FindCommandIndex(string[] args) =>
        Array.FindIndex(args, a => Commands.Contains(a, StringComparer.OrdinalIgnoreCase));

    private static int Usage(TextWriter output)
    {
        output.WriteLine("""
            Usage:
              sync-key issue  <hospitalCode>            (Cloud) issue a key; it is printed once
              sync-key list   <hospitalCode>            (Cloud)
              sync-key revoke <hospitalCode> [prefix]   (Cloud) revoke all keys, or the one with that prefix
              sync-cutover    <hospitalCode> on|off     (Cloud) patients managed by the local server
              sync-backfill   <hospitalCode>            (Edge)  queue patients never synced before
            """);
        return 2;
    }

    private sealed class CommandLineException(string message) : Exception(message);
}
