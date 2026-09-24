using HMS.Modules.Radiology.Domain;

namespace HMS.Modules.Radiology.Application.Abstractions;

internal interface IXrayAiAnalysisRepository
{
    Task AddAsync(XrayAiAnalysis analysis, CancellationToken cancellationToken);

    Task<XrayAiAnalysis?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Every saved analysis of the given documents, newest first.</summary>
    Task<IReadOnlyList<XrayAiAnalysis>> ListByDocumentIdsAsync(IReadOnlyCollection<Guid> documentIds, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
