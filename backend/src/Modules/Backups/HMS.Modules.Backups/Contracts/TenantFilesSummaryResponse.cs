namespace HMS.Modules.Backups.Contracts;

/// <summary>What a hospital's documents &amp; images zip would contain right now — totals plus
/// one <see cref="TenantFileCategoryResponse"/> per upload kind (always all of them, zero or not).</summary>
public record TenantFilesSummaryResponse
{
    public int FileCount { get; init; }
    public long TotalSizeBytes { get; init; }
    public IReadOnlyList<TenantFileCategoryResponse> Categories { get; init; } = [];
}

/// <summary>One upload kind inside a <see cref="TenantFilesSummaryResponse"/>. <see cref="Key"/>
/// is also that kind's top-level folder name inside the zip.</summary>
public record TenantFileCategoryResponse
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public int FileCount { get; init; }
    public long SizeBytes { get; init; }
}
