using System.Text.Json;
using HMS.Modules.ActivityLog.Application;
using HMS.Modules.ActivityLog.Contracts;
using HMS.Modules.Billing.Application.Abstractions;
using HMS.Modules.Billing.Contracts;
using HMS.Modules.Billing.Domain;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Billing.Application;

/// <summary>Settings → Billing Approvals (ADR-136): whether discounts are allowed and whether
/// discounts and bill reverts need approval. Public for the same CS0051 reason as
/// <see cref="IInvoiceService"/>: BillingSettingsController takes it.</summary>
public interface IBillingSettingsService
{
    /// <summary>The saved settings, or <see cref="BillingApprovalSettings.Default"/> until a
    /// Super Admin first saves them. Read once per request scope.</summary>
    Task<BillingApprovalSettings> GetAsync(CancellationToken cancellationToken);

    Task<Result<BillingApprovalSettings>> UpdateAsync(BillingApprovalSettings request, Guid? actorId, CancellationToken cancellationToken);
}

internal class BillingSettingsService : IBillingSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IBillingSettingsRepository _repository;
    private readonly IActivityLogService _activityLog;
    private readonly ILogger<BillingSettingsService> _logger;
    private BillingApprovalSettings? _cached;

    public BillingSettingsService(IBillingSettingsRepository repository, IActivityLogService activityLog, ILogger<BillingSettingsService> logger)
    {
        _repository = repository;
        _activityLog = activityLog;
        _logger = logger;
    }

    public async Task<BillingApprovalSettings> GetAsync(CancellationToken cancellationToken)
    {
        if (_cached is not null)
        {
            return _cached;
        }

        var settings = await _repository.GetAsync(cancellationToken);
        _cached = settings is null ? BillingApprovalSettings.Default : Deserialize(settings.ApprovalSettingsJson);
        return _cached;
    }

    public async Task<Result<BillingApprovalSettings>> UpdateAsync(BillingApprovalSettings request, Guid? actorId, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var settings = await _repository.GetAsync(cancellationToken);
        var previous = settings is null ? BillingApprovalSettings.Default : Deserialize(settings.ApprovalSettingsJson);

        if (settings is null)
        {
            await _repository.AddAsync(BillingSettings.Create(json, actorId), cancellationToken);
        }
        else
        {
            settings.Update(json, actorId);
        }

        await _repository.SaveChangesAsync(cancellationToken);
        _cached = request;

        // Best-effort audit write (never throws — see IActivityLogService).
        await _activityLog.LogAsync(new ActivityLogRequest
        {
            Action = ActivityLogActions.Update,
            Module = ActivityLogModules.Billing,
            EntityType = "BillingSettings",
            EntityId = "billing-settings",
            Description = "Updated the billing approval settings",
            OldValues = previous,
            NewValues = request,
            UserId = actorId,
        }, cancellationToken);

        return Result<BillingApprovalSettings>.Success(request);
    }

    // A row that no longer parses falls back to the defaults (approvals on) rather than breaking
    // billing — the next save rewrites it.
    private BillingApprovalSettings Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<BillingApprovalSettings>(json, JsonOptions) ?? BillingApprovalSettings.Default;
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Stored billing settings could not be read; using the defaults.");
            return BillingApprovalSettings.Default;
        }
    }
}
