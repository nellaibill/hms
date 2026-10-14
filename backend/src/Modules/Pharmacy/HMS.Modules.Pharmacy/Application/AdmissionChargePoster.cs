using HMS.Modules.IPD.Application;
using HMS.Modules.IPD.Contracts;

namespace HMS.Modules.Pharmacy.Application;

/// <summary>
/// The pharmacy's side of "put it on the admission". An inpatient's medicines are usually not
/// paid for at the counter: the bill is made, the stock leaves, and the amount joins the
/// admission's charges to be settled with the hospital bill at discharge.
/// </summary>
internal interface IAdmissionChargePoster
{
    /// <summary>Null when a bill for this patient can be charged to this admission; otherwise
    /// the reason it cannot.</summary>
    Task<string?> ValidateAsync(Guid admissionId, Guid patientId, CancellationToken cancellationToken);

    Task PostSaleAsync(Guid admissionId, string billNo, decimal amount, Guid? actorId, CancellationToken cancellationToken);

    Task PostSaleReturnAsync(Guid admissionId, string returnNo, string billNo, decimal amount, Guid? actorId, CancellationToken cancellationToken);
}

internal class AdmissionChargePoster : IAdmissionChargePoster
{
    private readonly IAdmissionService _admissionService;
    private readonly IAdmissionChargeService _admissionChargeService;

    public AdmissionChargePoster(IAdmissionService admissionService, IAdmissionChargeService admissionChargeService)
    {
        _admissionService = admissionService;
        _admissionChargeService = admissionChargeService;
    }

    public async Task<string?> ValidateAsync(Guid admissionId, Guid patientId, CancellationToken cancellationToken)
    {
        var result = await _admissionService.GetByIdAsync(admissionId, cancellationToken);
        if (!result.IsSuccess)
        {
            return "The admission was not found.";
        }

        var admission = result.Value!;
        if (admission.PatientId != patientId)
        {
            return $"Admission {admission.AdmissionNumber} belongs to a different patient.";
        }

        // Once discharged the charges are final (and may already be invoiced): a new charge
        // would never be billed.
        return admission.Status == AdmissionStatus.Admitted
            ? null
            : $"Admission {admission.AdmissionNumber} is not currently admitted, so nothing more can be charged to it.";
    }

    public Task PostSaleAsync(Guid admissionId, string billNo, decimal amount, Guid? actorId, CancellationToken cancellationToken)
        => PostAsync(admissionId, amount, $"Pharmacy bill {billNo}", actorId, cancellationToken);

    public Task PostSaleReturnAsync(Guid admissionId, string returnNo, string billNo, decimal amount, Guid? actorId, CancellationToken cancellationToken)
        => PostAsync(admissionId, -amount, $"Pharmacy return {returnNo} against {billNo}", actorId, cancellationToken);

    private async Task PostAsync(Guid admissionId, decimal amount, string remarks, Guid? actorId, CancellationToken cancellationToken)
    {
        if (amount == 0)
        {
            return;
        }

        try
        {
            await _admissionChargeService.CreateAsync(
                admissionId,
                new CreateAdmissionChargeRequest { ChargeType = ChargeType.PharmacyCharge, Amount = amount, Remarks = remarks },
                actorId,
                cancellationToken);
        }
        catch (Exception)
        {
            // Best-effort, like the dispense-to-invoice step before it (ADR-028): the bill and
            // the stock movement are already committed and must stand. The bill stays marked
            // as charged to the admission, which is what the IP billing desk reconciles against.
        }
    }
}
