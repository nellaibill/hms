using FluentAssertions;
using HMS.Modules.Billing.Application;
using HMS.Modules.Billing.Contracts;
using HMS.Modules.IPD.Application;
using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Contracts;
using HMS.Shared.Kernel;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.IPD.Application;

public class IPDBillingServiceTests
{
    private readonly IAdmissionRepository _admissionRepository = Substitute.For<IAdmissionRepository>();
    private readonly IAdmissionChargeService _admissionChargeService = Substitute.For<IAdmissionChargeService>();
    private readonly IInvoiceService _invoiceService = Substitute.For<IInvoiceService>();
    private readonly IPatientService _patientService = Substitute.For<IPatientService>();
    private readonly IPDBillingService _sut;
    private readonly Guid _admissionId = Guid.NewGuid();
    private readonly Guid _patientId = Guid.NewGuid();

    public IPDBillingServiceTests()
    {
        _sut = new IPDBillingService(_admissionRepository, _admissionChargeService, _invoiceService, _patientService);

        _patientService.GetByIdAsync(_patientId, Arg.Any<CancellationToken>())
            .Returns(Result<PatientResponse>.Success(new PatientResponse { Id = _patientId, FirstName = "Aravind", LastName = "Nadar", Uhid = "NH20260001" }));
    }

    private Admission DischargedAdmission()
    {
        var admission = Admission.Create("ADM-2026-000001", _patientId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(-3), AdmissionType.Elective, "Observation", null);
        admission.Discharge(DateTime.UtcNow, DischargeType.Normal, "Recovered", null, null, null);
        return admission;
    }

    private static Result<InvoiceResponse> SuccessfulInvoice(decimal netAmount = 1000m) =>
        Result<InvoiceResponse>.Success(new InvoiceResponse { Id = Guid.NewGuid(), InvoiceNumber = "INV-2026-000001", NetAmount = netAmount });

    [Fact]
    public async Task GenerateFinalBillAsync_WhenAdmissionDoesNotExist_ReturnsNotFoundFailure()
    {
        _admissionRepository.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns((Admission?)null);

        var result = await _sut.GenerateFinalBillAsync(_admissionId, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.NotFound);
    }

    [Fact]
    public async Task GenerateFinalBillAsync_WhenStillAdmitted_ReturnsAdmissionNotDischargedFailure()
    {
        var admission = Admission.Create("ADM-2026-000001", _patientId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, AdmissionType.Elective, "Observation", null);
        _admissionRepository.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns(admission);

        var result = await _sut.GenerateFinalBillAsync(_admissionId, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.AdmissionNotDischarged);
        await _invoiceService.DidNotReceive().CreateAsync(Arg.Any<CreateInvoiceRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateFinalBillAsync_WhenNoChargesExist_ReturnsNoChargesToBillFailure()
    {
        var admission = DischargedAdmission();
        _admissionRepository.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns(admission);
        _admissionChargeService.GetByAdmissionIdAsync(_admissionId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<AdmissionChargeResponse>>.Success([]));

        var result = await _sut.GenerateFinalBillAsync(_admissionId, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.NoChargesToBill);
    }

    [Fact]
    public async Task GenerateFinalBillAsync_WhenFinalInvoiceAlreadySet_ReturnsFinalBillAlreadyGeneratedFailure()
    {
        var admission = DischargedAdmission();
        admission.SetFinalInvoiceId(Guid.NewGuid(), null);
        _admissionRepository.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns(admission);

        var result = await _sut.GenerateFinalBillAsync(_admissionId, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.FinalBillAlreadyGenerated);
        await _admissionChargeService.DidNotReceive().GetByAdmissionIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateFinalBillAsync_WithValidCharges_MapsToInpatientChargeLinesAndUsesPatientIdAsVisitId()
    {
        var admission = DischargedAdmission();
        _admissionRepository.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns(admission);
        _admissionChargeService.GetByAdmissionIdAsync(_admissionId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<AdmissionChargeResponse>>.Success(
            [
                new AdmissionChargeResponse { ChargeType = ChargeType.BedCharge, Amount = 500m, Remarks = "Bed charge - 2026-09-06 (Highest room tariff)" },
                new AdmissionChargeResponse { ChargeType = ChargeType.LabCharge, Amount = 250m, Remarks = "Lab: CBC" },
                new AdmissionChargeResponse { ChargeType = ChargeType.NursingCharge, Amount = 100m, Remarks = null },
            ]));
        _invoiceService.CreateAsync(Arg.Any<CreateInvoiceRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(SuccessfulInvoice(850m));

        var result = await _sut.GenerateFinalBillAsync(_admissionId, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalAmount.Should().Be(850m);
        await _invoiceService.Received(1).CreateAsync(
            Arg.Is<CreateInvoiceRequest>(r =>
                r.PatientId == _patientId &&
                r.VisitId == _patientId &&
                r.PatientName == "Aravind Nadar" &&
                r.PatientUhid == "NH20260001" &&
                r.Items.Count == 3 &&
                r.Items.All(i => i.BillingType == BillingType.InpatientCharge) &&
                r.Items.Any(i => i.ServiceId == "BedCharge: Bed charge - 2026-09-06 (Highest room tariff)" && i.UnitPrice == 500m) &&
                r.Items.Any(i => i.ServiceId == "LabCharge: Lab: CBC" && i.UnitPrice == 250m) &&
                r.Items.Any(i => i.ServiceId == "NursingCharge" && i.UnitPrice == 100m)),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
        admission.FinalInvoiceId.Should().NotBeNull();
        await _admissionRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateFinalBillAsync_WhenPatientCannotBeResolved_ReturnsInvalidPatientFailure()
    {
        var admission = DischargedAdmission();
        _admissionRepository.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns(admission);
        _admissionChargeService.GetByAdmissionIdAsync(_admissionId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<AdmissionChargeResponse>>.Success([new AdmissionChargeResponse { ChargeType = ChargeType.BedCharge, Amount = 500m }]));
        _patientService.GetByIdAsync(_patientId, Arg.Any<CancellationToken>()).Returns(Result<PatientResponse>.Failure("PATIENTS.NOT_FOUND", "not found"));

        var result = await _sut.GenerateFinalBillAsync(_admissionId, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.InvalidPatient);
    }

    [Fact]
    public async Task GenerateFinalBillAsync_WhenInvoiceServiceFails_PropagatesFailureAndDoesNotSetFinalInvoiceId()
    {
        var admission = DischargedAdmission();
        _admissionRepository.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns(admission);
        _admissionChargeService.GetByAdmissionIdAsync(_admissionId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<AdmissionChargeResponse>>.Success([new AdmissionChargeResponse { ChargeType = ChargeType.BedCharge, Amount = 500m }]));
        _invoiceService.CreateAsync(Arg.Any<CreateInvoiceRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Result<InvoiceResponse>.Failure("BILLING.EMPTY_INVOICE", "no items"));

        var result = await _sut.GenerateFinalBillAsync(_admissionId, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("BILLING.EMPTY_INVOICE");
        admission.FinalInvoiceId.Should().BeNull();
        await _admissionRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
