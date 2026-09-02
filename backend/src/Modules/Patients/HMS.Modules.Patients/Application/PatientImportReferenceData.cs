using HMS.Modules.Masters.Application;

namespace HMS.Modules.Patients.Application;

/// <summary>
/// Loads Masters' State list once per batch and caches District lookups per State as rows ask
/// for them — avoids re-querying Masters for every one of a batch's (potentially tens of
/// thousands of) rows when most rows share only a handful of distinct State/District values.
/// </summary>
internal sealed class PatientImportReferenceData
{
    private readonly IReadOnlyDictionary<string, Guid> _statesByName;
    private readonly IDistrictService _districtService;
    private readonly Dictionary<Guid, IReadOnlyDictionary<string, Guid>> _districtsByState = [];

    private PatientImportReferenceData(IReadOnlyDictionary<string, Guid> statesByName, IDistrictService districtService)
    {
        _statesByName = statesByName;
        _districtService = districtService;
    }

    public static async Task<PatientImportReferenceData> LoadAsync(IStateService stateService, IDistrictService districtService, CancellationToken cancellationToken)
    {
        var states = await stateService.GetAllAsync(cancellationToken);
        var byName = states
            .GroupBy(s => s.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        return new PatientImportReferenceData(byName, districtService);
    }

    public bool TryGetStateId(string name, out Guid stateId) => _statesByName.TryGetValue(name.Trim(), out stateId);

    public async Task<Guid?> FindDistrictIdAsync(Guid stateId, string name, CancellationToken cancellationToken)
    {
        if (!_districtsByState.TryGetValue(stateId, out var byName))
        {
            var districts = await _districtService.GetByStateIdAsync(stateId, cancellationToken);
            byName = districts
                .GroupBy(d => d.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);
            _districtsByState[stateId] = byName;
        }

        return byName.TryGetValue(name.Trim(), out var districtId) ? districtId : null;
    }
}
