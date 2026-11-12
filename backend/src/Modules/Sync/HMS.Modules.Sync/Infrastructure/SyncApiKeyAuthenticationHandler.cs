using System.Security.Claims;
using System.Text.Encodings.Web;
using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Sync.Application;
using HMS.Modules.Sync.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HMS.Modules.Sync.Infrastructure;

/// <summary>
/// The <c>HmsSync</c> authentication scheme (ADR-133), active on a Cloud server only. Accepts
/// <c>Authorization: HmsSync &lt;key&gt;</c>, looks the key up by its SHA-256 hash in
/// sync_registry.sync_clients (not revoked), checks the hospital is active, and yields a principal
/// carrying the hospital's TenantId. It grants nothing else: only the sync receiver endpoints
/// accept this scheme, and no user permission claims are ever issued for it.
/// </summary>
internal sealed class SyncApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private static readonly TimeSpan LastUsedResolution = TimeSpan.FromMinutes(5);

    private readonly SyncRegistryDbContext _registry;
    private readonly ITenantDirectory _tenantDirectory;
    private readonly SyncOptions _syncOptions;

    public SyncApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        SyncRegistryDbContext registry,
        ITenantDirectory tenantDirectory,
        IOptions<SyncOptions> syncOptions)
        : base(options, logger, encoder)
    {
        _registry = registry;
        _tenantDirectory = tenantDirectory;
        _syncOptions = syncOptions.Value;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!SyncAuthenticationDefaults.IsSyncAuthorizationHeader(header))
        {
            return AuthenticateResult.NoResult();
        }

        if (!_syncOptions.IsCloud)
        {
            return AuthenticateResult.Fail("This server does not accept sync traffic.");
        }

        var key = header[(SyncAuthenticationDefaults.Scheme.Length + 1)..].Trim();
        if (!SyncApiKeys.IsWellFormed(key))
        {
            return AuthenticateResult.Fail("Malformed sync key.");
        }

        var hash = SyncApiKeys.Hash(key);
        var client = await _registry.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.KeyHash == hash && c.RevokedAt == null, Context.RequestAborted);
        if (client is null)
        {
            Logger.LogWarning("Sync request with an unknown or revoked key from {RemoteIp}.", Context.Connection.RemoteIpAddress);
            return AuthenticateResult.Fail("Unknown or revoked sync key.");
        }

        var tenant = await _tenantDirectory.FindByIdAsync(client.TenantId, Context.RequestAborted);
        if (tenant is null || !tenant.IsActive)
        {
            return AuthenticateResult.Fail("This hospital is not available.");
        }

        var now = DateTime.UtcNow;
        if (client.LastUsedAt is null || now - client.LastUsedAt > LastUsedResolution)
        {
            var cutoff = now - LastUsedResolution;
            await _registry.Clients
                .Where(c => c.Id == client.Id && (c.LastUsedAt == null || c.LastUsedAt < cutoff))
                .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.LastUsedAt, now), Context.RequestAborted);
        }

        var identity = new ClaimsIdentity(
            [
                new Claim(SyncAuthenticationDefaults.SyncClientIdClaim, client.Id.ToString()),
                new Claim(SyncAuthenticationDefaults.TenantIdClaim, tenant.Id.ToString()),
                new Claim(SyncAuthenticationDefaults.HospitalCodeClaim, tenant.HospitalCode),
            ],
            SyncAuthenticationDefaults.Scheme);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SyncAuthenticationDefaults.Scheme));
    }
}
