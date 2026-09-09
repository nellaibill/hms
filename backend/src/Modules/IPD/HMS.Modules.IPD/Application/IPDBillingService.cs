using HMS.Modules.Billing.Application;
using HMS.Modules.Billing.Contracts;
using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.Patients.Application;
using HMS.Shared.Kernel;

namespace HMS.Modules.IPD.Application;

/// <summary>
/// Public (not internal): IPDBillingController — which ASP.NET Core requires to be a public
/// class with a public constructor for controller discovery/DI activation — takes this as a
/// constructor dependency; a public constructor cannot have an internal parameter type
/// (CS0051). Converts a discharged admission's AdmissionCharge ledger into a real
/// HMS.Modules.Billing Invoice via IInvoiceService.CreateAsync — the actual payment-collection
/// point IPD has never had. See ADR-066.
/// </summary>
public interface IIPDBillingService
{
    Task<Result<GenerateFinalBillResponse>> GenerateFinalBillAsync(Guid admissionId, Guid? actorId, CancellationToken cancellationToken);
}

internal class IPDBillingService : IIPDBillingService
{
    private readonly IAdmissionRepository _admissionRepository;
    private readonly IAdmissionChargeService _admissionChargeService;
    private readonly IInvoiceService _invoiceService;
    private readonly IPatientService _patientService;

    public IPDBillingService(
        IAdmissionRepository admissionRepository,
        IAdmissionChargeService admissionChargeService,
        IInvoiceService invoiceService,
        IPatientService patientService)
    {
        _admissionRepository = admissionRepository;
        _admissionChargeService = admissionChargeService;
        _invoiceService = invoiceService;
        _patientService = patientService;
    }

    public async Task<Result<GenerateFinalBillResponse>> GenerateFinalBillAsync(Guid admissionId, Guid? actorId, CancellationToken cancellationToken)
    {
        var admission = await _admissionRepository.GetByIdAsync(admissionId, cancellationToken);
        if (admission is null)
        {
            return Result<GenerateFinalBillResponse>.Failure(IPDErrorCodes.NotFound, $"Admission '{admissionId}' was not found.");
        }

        if (admission.Status != AdmissionStatus.Discharged)
        {
            return Result<GenerateFinalBillResponse>.Failure(
                IPDErrorCodes.AdmissionNotDischarged,
                "A final bill can only be generated once the admission has been discharged (charges, such as the final bed-day charge, are only finalized at discharge).");
        }

        if (admission.FinalInvoiceId is not null)
        {
            return Result<GenerateFinalBillResponse>.Failure(
                IPDErrorCodes.FinalBillAlreadyGenerated,
                $"A final bill has already been generated for admission '{admissionId}'.");
        }

        var chargesResult = await _admissionChargeService.GetByAdmissionIdAsync(admissionId, cancellationToken);
        if (!chargesResult.IsSuccess)
        {
            return Result<GenerateFinalBillResponse>.Failure(chargesResult.ErrorCode!, chargesResult.Error!);
        }

        var charges = chargesResult.Value!;
        if (charges.Count == 0)
        {
            return Result<GenerateFinalBillResponse>.Failure(IPDErrorCodes.NoChargesToBill, "This admission has no charges to bill.");
        }

        var patientResult = await _patientService.GetByIdAsync(admission.PatientId, cancellationToken);
        if (!patientResult.IsSuccess)
        {
            return Result<GenerateFinalBillResponse>.Failure(IPDErrorCodes.InvalidPatient, $"Patient '{admission.PatientId}' was not found.");
        }

        var lines = charges.Select(charge => new CreateInvoiceLineItemRequest
        {
            BillingType = BillingType.InpatientCharge,
            ServiceId = string.IsNullOrWhiteSpace(charge.Remarks) ? charge.ChargeType.ToString() : $"{charge.ChargeType}: {charge.Remarks}",
            Quantity = 1,
            UnitPrice = charge.Amount,
        }).ToList();

        var invoiceResult = await _invoiceService.CreateAsync(
            new CreateInvoiceRequest
            {
                PatientId = admission.PatientId,
                // IPD has no Visit concept — this is the codebase's own documented fallback for
                // flows with no real visit (see CreateInvoiceRequest.VisitId's doc comment).
                VisitId = admission.PatientId,
                PatientName = $"{patientResult.Value!.FirstName} {patientResult.Value.LastName}".Trim(),
                PatientUhid = patientResult.Value.Uhid,
                Items = lines,
            },
            actorId,
            cancellationToken);

        if (!invoiceResult.IsSuccess)
        {
            return Result<GenerateFinalBillResponse>.Failure(invoiceResult.ErrorCode!, invoiceResult.Error!);
        }

        admission.SetFinalInvoiceId(invoiceResult.Value!.Id, actorId);
        await _admissionRepository.SaveChangesAsync(cancellationToken);

        return Result<GenerateFinalBillResponse>.Success(new GenerateFinalBillResponse
        {
            InvoiceId = invoiceResult.Value.Id,
            InvoiceNumber = invoiceResult.Value.InvoiceNumber,
            TotalAmount = invoiceResult.Value.NetAmount,
        });
    }
}
