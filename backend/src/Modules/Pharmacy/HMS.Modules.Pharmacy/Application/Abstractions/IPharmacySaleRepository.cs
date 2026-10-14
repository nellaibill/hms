using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;

namespace HMS.Modules.Pharmacy.Application.Abstractions;

/// <summary>Bills, returns against them, and bills parked at the counter. Nothing here
/// saves: everything is committed through IPharmacyUnitOfWork.</summary>
internal interface IPharmacySaleRepository
{
    Task AddAsync(PharmacySale sale, CancellationToken cancellationToken);

    /// <summary>The bill with its lines and payments, tracked.</summary>
    Task<PharmacySale?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Headers only, newest first, with each bill's line count.</summary>
    Task<(IReadOnlyList<(PharmacySale Sale, int LineCount)> Items, int TotalCount)> GetPagedAsync(SaleListQuery query, CancellationToken cancellationToken);

    /// <summary>A patient's bills with their lines and payments, newest first, at most
    /// <paramref name="max"/> of them.</summary>
    Task<IReadOnlyList<PharmacySale>> GetForPatientAsync(Guid patientId, int max, CancellationToken cancellationToken);

    /// <summary>Returns, with their lines, made against any of the given bills.</summary>
    Task<IReadOnlyList<PharmacySaleReturn>> GetReturnsForSalesAsync(IReadOnlyCollection<Guid> saleIds, CancellationToken cancellationToken);

    /// <summary>The bills made against each of the given prescriptions (OPD consultation ids).</summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<PrescriptionBillResponse>>> GetBillsForPrescriptionsAsync(IReadOnlyCollection<Guid> prescriptionIds, CancellationToken cancellationToken);

    Task AddReturnAsync(PharmacySaleReturn saleReturn, CancellationToken cancellationToken);

    Task<PharmacySaleReturn?> GetReturnByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Returns with their lines, newest first.</summary>
    Task<(IReadOnlyList<PharmacySaleReturn> Items, int TotalCount)> GetReturnsPagedAsync(SaleReturnListQuery query, CancellationToken cancellationToken);

    Task AddHeldAsync(PharmacyHeldBill heldBill, CancellationToken cancellationToken);

    Task<PharmacyHeldBill?> GetHeldAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<PharmacyHeldBill>> GetHeldForStoreAsync(Guid storeId, CancellationToken cancellationToken);

    /// <summary>A held bill is scrap paper: once billed or discarded it is deleted outright.</summary>
    void RemoveHeld(PharmacyHeldBill heldBill);
}
