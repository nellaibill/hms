using HMS.Modules.Patients.Application;
using HMS.Modules.Products.Application;
using HMS.Modules.Products.Contracts;

namespace HMS.Modules.Pharmacy.Application;

/// <summary>
/// Resolves the display fields a Pharmacy list denormalizes from other modules (product name,
/// batch number/expiry, patient name) for a whole page at once — one Products query for all
/// the page's products and one for all its batches, instead of a round-trip per row.
/// </summary>
internal static class PharmacyLookups
{
    public static async Task<IReadOnlyDictionary<Guid, ProductResponse>> GetLookupAsync(
        this IProductService productService, IEnumerable<Guid> productIds, CancellationToken cancellationToken)
    {
        var products = await productService.GetByIdsAsync(productIds.Distinct().ToList(), cancellationToken);
        return products.ToDictionary(p => p.Id);
    }

    public static async Task<IReadOnlyDictionary<Guid, ProductBatchResponse>> GetLookupAsync(
        this IProductBatchService productBatchService, IEnumerable<Guid> productBatchIds, CancellationToken cancellationToken)
    {
        var batches = await productBatchService.GetByIdsAsync(productBatchIds.Distinct().ToList(), cancellationToken);
        return batches.ToDictionary(b => b.Id);
    }

    /// <summary>Patients has no bulk lookup on its public seam, so this is still one call per
    /// patient — but per distinct patient on the page, not per row.</summary>
    public static async Task<IReadOnlyDictionary<Guid, string>> GetNameLookupAsync(
        this IPatientService patientService, IEnumerable<Guid?> patientIds, CancellationToken cancellationToken)
    {
        var names = new Dictionary<Guid, string>();
        foreach (var patientId in patientIds.OfType<Guid>().Distinct())
        {
            var patient = (await patientService.GetByIdAsync(patientId, cancellationToken)).Value;
            if (patient is not null)
            {
                names[patientId] = $"{patient.FirstName} {patient.LastName}";
            }
        }

        return names;
    }
}
