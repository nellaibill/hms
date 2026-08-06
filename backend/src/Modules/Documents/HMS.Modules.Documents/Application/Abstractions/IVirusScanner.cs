namespace HMS.Modules.Documents.Application.Abstractions;

internal enum ScanOutcome
{
    Clean = 0,
    Infected = 1,
}

internal readonly record struct ScanResult(ScanOutcome Outcome, string? SignatureName);

/// <summary>
/// Scans a stored file's content for malicious content before it can be marked
/// <see cref="Contracts.DocumentStatus.Available"/> — see US-9. No real antivirus engine
/// (e.g. ClamAV) is wired into this codebase yet; the registered implementation is
/// Infrastructure.NullVirusScanner, a clearly-labeled stub. Swapping in a real engine later
/// means implementing this interface again — nothing above it changes.
/// </summary>
internal interface IVirusScanner
{
    Task<ScanResult> ScanAsync(Stream content, CancellationToken cancellationToken);
}
