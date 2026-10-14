using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Application;

internal static class PharmacySettingsReader
{
    /// <summary>The expiry alert window in force: the saved setting, or — until one is saved —
    /// the same default the settings screen shows.</summary>
    public static async Task<int> ExpiryAlertDaysAsync(this IPharmacySettingsRepository repository, CancellationToken cancellationToken)
        => (await repository.GetAsync(cancellationToken))?.ExpiryAlertDays ?? PharmacySettingsDefaults.ExpiryAlertDays;
}

public interface ISettingsService
{
    Task<PharmacySettingsResponse> GetAsync(CancellationToken cancellationToken);

    Task<Result<PharmacySettingsResponse>> UpdateAsync(UpdatePharmacySettingsRequest request, Guid? actorId, CancellationToken cancellationToken);
}

/// <summary>The pharmacy's own particulars — what a bill must carry by law (GSTIN, drug
/// licence) and the few choices that shape how the counter works.</summary>
internal class SettingsService : ISettingsService
{
    private readonly IPharmacySettingsRepository _repository;
    private readonly IPharmacyUnitOfWork _unitOfWork;

    public SettingsService(IPharmacySettingsRepository repository, IPharmacyUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PharmacySettingsResponse> GetAsync(CancellationToken cancellationToken)
        => ToResponse(await _repository.GetAsync(cancellationToken));

    public async Task<Result<PharmacySettingsResponse>> UpdateAsync(UpdatePharmacySettingsRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var settings = await _repository.GetAsync(cancellationToken);
        if (settings is null)
        {
            settings = PharmacySettings.Create(actorId);
            await _repository.AddAsync(settings, cancellationToken);
        }

        settings.Update(
            request.PharmacyName, request.Gstin, request.DrugLicenceNo, request.Address, request.Phone, request.BillFooter,
            request.PrintFormat, request.MaxDiscountPercent, request.ExpiryAlertDays, actorId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<PharmacySettingsResponse>.Success(ToResponse(settings));
    }

    /// <summary>Until someone saves the settings there is no row; the defaults stand in.</summary>
    internal static PharmacySettingsResponse ToResponse(PharmacySettings? settings) => settings is null
        ? new PharmacySettingsResponse()
        : new PharmacySettingsResponse
        {
            PharmacyName = settings.PharmacyName,
            Gstin = settings.Gstin,
            DrugLicenceNo = settings.DrugLicenceNo,
            Address = settings.Address,
            Phone = settings.Phone,
            BillFooter = settings.BillFooter,
            PrintFormat = settings.PrintFormat,
            MaxDiscountPercent = settings.MaxDiscountPercent,
            ExpiryAlertDays = settings.ExpiryAlertDays,
        };
}
