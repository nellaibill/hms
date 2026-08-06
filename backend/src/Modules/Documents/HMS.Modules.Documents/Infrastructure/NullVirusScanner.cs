using HMS.Modules.Documents.Application.Abstractions;

namespace HMS.Modules.Documents.Infrastructure;

/// <summary>
/// Stub scan engine — always reports <see cref="ScanOutcome.Clean"/>. No real antivirus
/// engine (ClamAV or otherwise) is integrated anywhere in this codebase; this exists so the
/// rest of the pipeline (queueing, status transitions, the Pending/Available/Quarantined
/// state machine — US-9) is real and testable today, with a single, clearly-labeled seam to
/// replace once a real engine is wired in. Do not treat a document reaching
/// <see cref="Application.Abstractions.ScanOutcome.Clean"/> as evidence it was actually scanned
/// for malware while this implementation is registered — see
/// docs/modules/Documents/DocumentManagement.md's Risks section.
/// </summary>
internal class NullVirusScanner : IVirusScanner
{
    public Task<ScanResult> ScanAsync(Stream content, CancellationToken cancellationToken)
        => Task.FromResult(new ScanResult(ScanOutcome.Clean, SignatureName: null));
}
