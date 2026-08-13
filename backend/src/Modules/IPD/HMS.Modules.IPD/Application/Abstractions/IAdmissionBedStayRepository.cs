using HMS.Modules.IPD.Domain;

namespace HMS.Modules.IPD.Application.Abstractions;

/// <summary>
/// No SaveChangesAsync — stays are always written inside AdmissionService's admit/transfer/
/// discharge methods, persisted via IAdmissionRepository.SaveChangesAsync on the same DbContext.
/// </summary>
internal interface IAdmissionBedStayRepository
{
    Task AddAsync(AdmissionBedStay stay, CancellationToken cancellationToken);

    Task<AdmissionBedStay?> GetActiveByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AdmissionBedStay>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken);
}
