namespace HMS.Modules.Backups.Application.Abstractions;

/// <summary>
/// One hospital's uploaded files (documents and images) — the counterpart to the daily pg_dump
/// in <see cref="IBackupStorage"/>, since a database dump only carries the <em>paths</em> those
/// files live at, never their bytes. Built on demand from whatever is on disk right now rather
/// than on a nightly schedule: files are append-mostly, a daily copy of every tenant's full
/// upload tree would multiply disk use by the retention window, and a hospital downloading its
/// files wants the current set, not last night's.
/// </summary>
public interface ITenantFilesArchive
{
    /// <summary>What the tenant's archive would contain right now, grouped by upload kind —
    /// every kind is always listed (a zero count means "nothing uploaded of this kind yet").</summary>
    Task<TenantFilesSummary> GetSummaryAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>Writes a zip of every file belonging to <paramref name="tenantId"/> into
    /// <paramref name="destination"/>, one top-level folder per upload kind, leaving the stream
    /// open. Returns the number of files written.</summary>
    Task<int> WriteZipAsync(Guid tenantId, Stream destination, CancellationToken cancellationToken);
}

public sealed record TenantFilesSummary(IReadOnlyList<TenantFileCategory> Categories)
{
    public int FileCount => Categories.Sum(category => category.FileCount);

    public long TotalSizeBytes => Categories.Sum(category => category.SizeBytes);
}

public sealed record TenantFileCategory(string Key, string Label, int FileCount, long SizeBytes);
