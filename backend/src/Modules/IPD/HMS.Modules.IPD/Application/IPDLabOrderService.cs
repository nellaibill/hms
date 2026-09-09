using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.Laboratory.Application;
using HMS.Modules.Laboratory.Contracts;
using HMS.Modules.Masters.Application;
using HMS.Modules.Patients.Application;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.IPD.Application;

/// <summary>
/// Public (not internal): IPDLabOrdersController — which ASP.NET Core requires to be a public
/// class with a public constructor for controller discovery/DI activation — takes this as a
/// constructor dependency; a public constructor cannot have an internal parameter type
/// (CS0051). Orchestrates a real HMS.Modules.Laboratory.LabOrder directly from an IPD
/// admission (ILabOrderService.CreateFromAdmissionAsync — no invoice required, see ADR-064)
/// plus one AdmissionCharge per line so the cost is visible on the Charges tab even though IPD
/// has no real billing/invoicing yet (same category of deferral as ADR-062/063).
/// </summary>
public interface IIPDLabOrderService
{
    Task<Result<LabOrderResponse>> PlaceOrderAsync(Guid admissionId, CreatePlaceLabOrderRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<LabOrderResponse>>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken);
}

internal class IPDLabOrderService : IIPDLabOrderService
{
    private readonly IAdmissionRepository _admissionRepository;
    private readonly ILabOrderService _labOrderService;
    private readonly IDiagnosticServiceService _diagnosticServiceService;
    private readonly IDiagnosticPackageService _diagnosticPackageService;
    private readonly IAdmissionChargeService _admissionChargeService;
    private readonly IPatientService _patientService;
    private readonly ILogger<IPDLabOrderService> _logger;

    public IPDLabOrderService(
        IAdmissionRepository admissionRepository,
        ILabOrderService labOrderService,
        IDiagnosticServiceService diagnosticServiceService,
        IDiagnosticPackageService diagnosticPackageService,
        IAdmissionChargeService admissionChargeService,
        IPatientService patientService,
        ILogger<IPDLabOrderService> logger)
    {
        _admissionRepository = admissionRepository;
        _labOrderService = labOrderService;
        _diagnosticServiceService = diagnosticServiceService;
        _diagnosticPackageService = diagnosticPackageService;
        _admissionChargeService = admissionChargeService;
        _patientService = patientService;
        _logger = logger;
    }

    public async Task<Result<LabOrderResponse>> PlaceOrderAsync(Guid admissionId, CreatePlaceLabOrderRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var admission = await _admissionRepository.GetByIdAsync(admissionId, cancellationToken);
        if (admission is null)
        {
            return Result<LabOrderResponse>.Failure(IPDErrorCodes.NotFound, $"Admission '{admissionId}' was not found.");
        }

        var patientResult = await _patientService.GetByIdAsync(admission.PatientId, cancellationToken);
        if (!patientResult.IsSuccess)
        {
            return Result<LabOrderResponse>.Failure(IPDErrorCodes.InvalidPatient, $"Patient '{admission.PatientId}' was not found.");
        }

        var labResult = await _labOrderService.CreateFromAdmissionAsync(
            new CreateLabOrderFromAdmissionRequest
            {
                AdmissionId = admissionId,
                PatientId = admission.PatientId,
                PatientName = $"{patientResult.Value!.FirstName} {patientResult.Value.LastName}".Trim(),
                PatientUhid = patientResult.Value.Uhid,
                Lines = request.Lines.Select(l => new CreateLabOrderLineFromAdmissionRequest
                {
                    ServiceId = l.ServiceId,
                    PackageId = l.PackageId,
                }).ToList(),
            },
            actorId,
            cancellationToken);

        if (!labResult.IsSuccess)
        {
            return Result<LabOrderResponse>.Failure(labResult.ErrorCode!, labResult.Error!);
        }

        // Best-effort, not transactional with the lab order itself — the order is the source
        // of truth; a charge-posting failure must never undo/fail an already-placed order.
        // Mirrors Billing's InvoiceService "downstream posting must never fail the primary
        // action" precedent (see InvoiceService.CreateAsync's own Laboratory call site).
        foreach (var line in request.Lines)
        {
            await PostChargeAsync(admissionId, line, actorId, cancellationToken);
        }

        return labResult;
    }

    public async Task<Result<IReadOnlyList<LabOrderResponse>>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken)
    {
        if (await _admissionRepository.GetByIdAsync(admissionId, cancellationToken) is null)
        {
            return Result<IReadOnlyList<LabOrderResponse>>.Failure(IPDErrorCodes.NotFound, $"Admission '{admissionId}' was not found.");
        }

        return await _labOrderService.GetByAdmissionIdAsync(admissionId, cancellationToken);
    }

    private async Task PostChargeAsync(Guid admissionId, PlaceLabOrderLineRequest line, Guid? actorId, CancellationToken cancellationToken)
    {
        try
        {
            decimal amount;
            string remarks;

            if (line.PackageId is { } packageId)
            {
                var packageResult = await _diagnosticPackageService.GetByIdAsync(packageId, cancellationToken);
                if (!packageResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "IPD: could not resolve package '{PackageId}' to post a Lab charge for admission '{AdmissionId}' — skipping charge.",
                        packageId, admissionId);
                    return;
                }

                amount = packageResult.Value!.TotalPrice;
                remarks = $"Lab: {packageResult.Value.Name}";
            }
            else if (line.ServiceId is { } serviceId)
            {
                var serviceResult = await _diagnosticServiceService.GetByIdAsync(serviceId, cancellationToken);
                if (!serviceResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "IPD: could not resolve service '{ServiceId}' to post a Lab charge for admission '{AdmissionId}' — skipping charge.",
                        serviceId, admissionId);
                    return;
                }

                amount = serviceResult.Value!.Price;
                remarks = $"Lab: {serviceResult.Value.Name}";
            }
            else
            {
                return;
            }

            var chargeResult = await _admissionChargeService.CreateAsync(
                admissionId,
                new CreateAdmissionChargeRequest { ChargeType = ChargeType.LabCharge, Amount = amount, Remarks = remarks },
                actorId,
                cancellationToken);

            if (!chargeResult.IsSuccess)
            {
                _logger.LogWarning("IPD: failed to post a Lab charge for admission '{AdmissionId}': {Error}", admissionId, chargeResult.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "IPD: unexpected error posting a Lab charge for admission '{AdmissionId}'.", admissionId);
        }
    }
}
