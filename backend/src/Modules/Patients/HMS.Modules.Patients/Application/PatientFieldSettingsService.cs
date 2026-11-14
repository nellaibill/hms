using System.Text.Json;
using HMS.Modules.ActivityLog.Application;
using HMS.Modules.ActivityLog.Contracts;
using HMS.Modules.Patients.Application.Abstractions;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Patients.Application;

/// <summary>Settings → Patient Registration Fields (ADR-135): which registration fields the
/// hospital makes mandatory.</summary>
public interface IPatientFieldSettingsService
{
    /// <summary>The saved choices, or <see cref="PatientFieldRequirements.Default"/> until a
    /// Super Admin first saves them. Read once per request scope — the patient validators
    /// and the emergency-contact removal all ask for it.</summary>
    Task<PatientFieldRequirements> GetAsync(CancellationToken cancellationToken);

    Task<Result<PatientFieldRequirements>> UpdateAsync(PatientFieldRequirements request, Guid? actorId, CancellationToken cancellationToken);
}

internal class PatientFieldSettingsService : IPatientFieldSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IPatientFieldSettingsRepository _repository;
    private readonly IActivityLogService _activityLog;
    private readonly ILogger<PatientFieldSettingsService> _logger;
    private PatientFieldRequirements? _cached;

    public PatientFieldSettingsService(IPatientFieldSettingsRepository repository, IActivityLogService activityLog, ILogger<PatientFieldSettingsService> logger)
    {
        _repository = repository;
        _activityLog = activityLog;
        _logger = logger;
    }

    public async Task<PatientFieldRequirements> GetAsync(CancellationToken cancellationToken)
    {
        if (_cached is not null)
        {
            return _cached;
        }

        var settings = await _repository.GetAsync(cancellationToken);
        _cached = settings is null ? PatientFieldRequirements.Default : Deserialize(settings.RequiredFieldsJson);
        return _cached;
    }

    public async Task<Result<PatientFieldRequirements>> UpdateAsync(PatientFieldRequirements request, Guid? actorId, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var settings = await _repository.GetAsync(cancellationToken);
        var previous = settings is null ? PatientFieldRequirements.Default : Deserialize(settings.RequiredFieldsJson);

        if (settings is null)
        {
            await _repository.AddAsync(PatientFieldSettings.Create(json, actorId), cancellationToken);
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
            Module = ActivityLogModules.Patients,
            EntityType = "PatientFieldSettings",
            EntityId = "patient-field-settings",
            Description = "Updated the mandatory patient registration fields",
            OldValues = previous,
            NewValues = request,
            UserId = actorId,
        }, cancellationToken);

        return Result<PatientFieldRequirements>.Success(request);
    }

    // A row that no longer parses (hand-edited, or from a future version) falls back to the
    // defaults rather than breaking registration — the next save rewrites it.
    private PatientFieldRequirements Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<PatientFieldRequirements>(json, JsonOptions) ?? PatientFieldRequirements.Default;
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Stored patient field settings could not be read; using the defaults.");
            return PatientFieldRequirements.Default;
        }
    }
}
