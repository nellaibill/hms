using FluentAssertions;
using HMS.Modules.Laboratory.Application;
using HMS.Modules.Laboratory.Application.Abstractions;
using HMS.Modules.Laboratory.Contracts;
using HMS.Modules.Laboratory.Domain;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Contracts;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Laboratory.Application;

public class LabOrderServiceTests
{
    private readonly ILabOrderRepository _repository = Substitute.For<ILabOrderRepository>();
    private readonly ILabOrderNumberGenerator _numberGenerator = Substitute.For<ILabOrderNumberGenerator>();
    private readonly IDiagnosticServiceService _diagnosticServiceService = Substitute.For<IDiagnosticServiceService>();
    private readonly IDiagnosticPackageService _diagnosticPackageService = Substitute.For<IDiagnosticPackageService>();
    private readonly ILogger<LabOrderService> _logger = Substitute.For<ILogger<LabOrderService>>();
    private readonly LabOrderService _sut;

    public LabOrderServiceTests()
    {
        _sut = new LabOrderService(_repository, _numberGenerator, _diagnosticServiceService, _diagnosticPackageService, _logger);
        _numberGenerator.NextLabOrderNumberAsync(Arg.Any<CancellationToken>()).Returns("LAB-2026-000001");
    }

    private static LabOrder BuildOrder(out Guid itemId, LabOrderItemStatus initialStatus = LabOrderItemStatus.PendingCollection)
    {
        var spec = new LabOrderItemSpec(Guid.NewGuid(), null, "Complete Blood Count", Guid.NewGuid(), null, null, null);
        var order = LabOrder.Create(
            "LAB-2026-000001",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Aravind Nadar",
            "NH20260001",
            Guid.NewGuid(),
            "OP",
            [spec],
            createdBy: null);

        itemId = order.Items.Single().Id;

        if (initialStatus != LabOrderItemStatus.PendingCollection)
        {
            throw new ArgumentException("Only PendingCollection is supported as a fresh build; drive the item through its mutators instead.", nameof(initialStatus));
        }

        return order;
    }

    private static CreateLabOrderFromInvoiceRequest ValidRequest(Guid invoiceId, Guid serviceId) => new()
    {
        InvoiceId = invoiceId,
        PatientId = Guid.NewGuid(),
        PatientName = "Aravind Nadar",
        PatientUhid = "NH20260001",
        VisitId = Guid.NewGuid(),
        Source = "OP",
        Lines = [new CreateLabOrderLineRequest { InvoiceLineItemId = Guid.NewGuid(), ServiceId = serviceId }],
    };

    [Fact]
    public async Task CreateFromInvoiceAsync_WithStandaloneServiceLine_CreatesOneItem()
    {
        var invoiceId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        _diagnosticServiceService.GetByIdAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns(Result<DiagnosticServiceResponse>.Success(new DiagnosticServiceResponse { Id = serviceId, Name = "CBC", ServiceType = DiagnosticTestServiceType.Laboratory }));
        _repository.GetByInvoiceIdAsync(invoiceId, Arg.Any<CancellationToken>()).Returns((LabOrder?)null);

        var result = await _sut.CreateFromInvoiceAsync(ValidRequest(invoiceId, serviceId), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items.Single().ServiceId.Should().Be(serviceId);
        result.Value.Items.Single().TestName.Should().Be("CBC");
        result.Value.LabOrderNumber.Should().Be("LAB-2026-000001");
        await _repository.Received(1).AddAsync(Arg.Any<LabOrder>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateFromInvoiceAsync_WithPackageLine_ExpandsIntoItemsSharingPackageId()
    {
        var invoiceId = Guid.NewGuid();
        var packageId = Guid.NewGuid();
        var service1 = Guid.NewGuid();
        var service2 = Guid.NewGuid();

        _repository.GetByInvoiceIdAsync(invoiceId, Arg.Any<CancellationToken>()).Returns((LabOrder?)null);
        _diagnosticPackageService.GetByIdAsync(packageId, Arg.Any<CancellationToken>())
            .Returns(Result<DiagnosticPackageResponse>.Success(new DiagnosticPackageResponse
            {
                Id = packageId,
                Name = "Fever Panel",
                Items =
                [
                    new DiagnosticPackageItemResponse { ServiceId = service1 },
                    new DiagnosticPackageItemResponse { ServiceId = service2 },
                ],
            }));
        _diagnosticServiceService.GetByIdAsync(service1, Arg.Any<CancellationToken>())
            .Returns(Result<DiagnosticServiceResponse>.Success(new DiagnosticServiceResponse { Id = service1, Name = "CBC", ServiceType = DiagnosticTestServiceType.Laboratory }));
        _diagnosticServiceService.GetByIdAsync(service2, Arg.Any<CancellationToken>())
            .Returns(Result<DiagnosticServiceResponse>.Success(new DiagnosticServiceResponse { Id = service2, Name = "Widal Test", ServiceType = DiagnosticTestServiceType.Laboratory }));

        var request = new CreateLabOrderFromInvoiceRequest
        {
            InvoiceId = invoiceId,
            PatientId = Guid.NewGuid(),
            PatientName = "Aravind Nadar",
            PatientUhid = "NH20260001",
            VisitId = Guid.NewGuid(),
            Source = "OP",
            Lines = [new CreateLabOrderLineRequest { InvoiceLineItemId = Guid.NewGuid(), PackageId = packageId }],
        };

        var result = await _sut.CreateFromInvoiceAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.Items.Should().OnlyContain(i => i.PackageId == packageId);
        result.Value.Items.Select(i => i.ServiceId).Should().BeEquivalentTo([service1, service2]);
    }

    [Fact]
    public async Task CreateFromInvoiceAsync_CalledTwiceForSameInvoice_ReturnsExistingOrderInsteadOfDuplicating()
    {
        var invoiceId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        _diagnosticServiceService.GetByIdAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns(Result<DiagnosticServiceResponse>.Success(new DiagnosticServiceResponse { Id = serviceId, Name = "CBC", ServiceType = DiagnosticTestServiceType.Laboratory }));
        _repository.GetByInvoiceIdAsync(invoiceId, Arg.Any<CancellationToken>()).Returns((LabOrder?)null);

        LabOrder? captured = null;
        _repository.WhenForAnyArgs(r => r.AddAsync(default!, default)).Do(ci => captured = ci.Arg<LabOrder>());

        var request = ValidRequest(invoiceId, serviceId);
        var first = await _sut.CreateFromInvoiceAsync(request, actorId: null, CancellationToken.None);
        first.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();

        // Simulate the retried call now finding the already-persisted order.
        _repository.GetByInvoiceIdAsync(invoiceId, Arg.Any<CancellationToken>()).Returns(captured);

        var second = await _sut.CreateFromInvoiceAsync(request, actorId: null, CancellationToken.None);

        second.IsSuccess.Should().BeTrue();
        second.Value!.Id.Should().Be(first.Value!.Id);
        second.Value.LabOrderNumber.Should().Be(first.Value.LabOrderNumber);
        await _repository.Received(1).AddAsync(Arg.Any<LabOrder>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FullHappyPathLifecycle_DrivesOverallStatusThroughExpectedSequenceEndingVerified_ThenReportGenerationAndReleaseSucceedInOrder()
    {
        var order = BuildOrder(out var itemId);
        _repository.GetByItemIdAsync(itemId, Arg.Any<CancellationToken>()).Returns(order);
        _repository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var collected = await _sut.CollectSampleAsync(itemId, new CollectSampleRequest { SampleType = LabSampleType.Blood, Quantity = "5ml" }, actorId: null, CancellationToken.None);
        collected.IsSuccess.Should().BeTrue();
        collected.Value!.OverallStatus.Should().Be(LabOrderStatus.Collected);

        var received = await _sut.ReceiveSampleAsync(itemId, actorId: null, CancellationToken.None);
        received.IsSuccess.Should().BeTrue();
        received.Value!.OverallStatus.Should().Be(LabOrderStatus.Received);

        var processing = await _sut.StartProcessingAsync(itemId, actorId: null, CancellationToken.None);
        processing.IsSuccess.Should().BeTrue();
        processing.Value!.OverallStatus.Should().Be(LabOrderStatus.Processing);

        var draftSaved = await _sut.SaveResultDraftAsync(
            itemId,
            new SaveResultDraftRequest { Parameters = [new ResultParameterRequest { ParameterName = "Hemoglobin", ResultValue = "13.5", Unit = "g/dL" }] },
            actorId: null,
            CancellationToken.None);
        draftSaved.IsSuccess.Should().BeTrue();
        draftSaved.Value!.OverallStatus.Should().Be(LabOrderStatus.ResultEntryInProgress);

        var submitted = await _sut.SubmitForVerificationAsync(itemId, actorId: null, CancellationToken.None);
        submitted.IsSuccess.Should().BeTrue();
        submitted.Value!.OverallStatus.Should().Be(LabOrderStatus.PendingVerification);

        var verified = await _sut.VerifyAsync(itemId, actorId: null, CancellationToken.None);
        verified.IsSuccess.Should().BeTrue();
        verified.Value!.OverallStatus.Should().Be(LabOrderStatus.Verified);

        // Release before generate must fail.
        var releaseBeforeGenerate = await _sut.ReleaseReportAsync(order.Id, actorId: null, CancellationToken.None);
        releaseBeforeGenerate.IsSuccess.Should().BeFalse();
        releaseBeforeGenerate.ErrorCode.Should().Be(LaboratoryErrorCodes.ReportNotGenerated);

        var generated = await _sut.GenerateReportAsync(order.Id, actorId: null, CancellationToken.None);
        generated.IsSuccess.Should().BeTrue();
        generated.Value!.OverallStatus.Should().Be(LabOrderStatus.ReadyForRelease);

        var released = await _sut.ReleaseReportAsync(order.Id, actorId: null, CancellationToken.None);
        released.IsSuccess.Should().BeTrue();
        released.Value!.OverallStatus.Should().Be(LabOrderStatus.Released);

        // A second release must fail.
        var releasedAgain = await _sut.ReleaseReportAsync(order.Id, actorId: null, CancellationToken.None);
        releasedAgain.IsSuccess.Should().BeFalse();
        releasedAgain.ErrorCode.Should().Be(LaboratoryErrorCodes.AlreadyReleased);
    }

    [Fact]
    public async Task RejectSample_ThenRequestRecollection_ThenCollectAgain_Succeeds()
    {
        var order = BuildOrder(out var itemId);
        _repository.GetByItemIdAsync(itemId, Arg.Any<CancellationToken>()).Returns(order);

        await _sut.CollectSampleAsync(itemId, new CollectSampleRequest { SampleType = LabSampleType.Blood }, actorId: null, CancellationToken.None);

        var rejected = await _sut.RejectSampleAsync(itemId, new RejectSampleRequest { Reason = LabSampleRejectionReason.HemolyzedSample, Remarks = "Sample hemolyzed in transit" }, actorId: null, CancellationToken.None);
        rejected.IsSuccess.Should().BeTrue();
        rejected.Value!.Items.Single().Status.Should().Be(LabOrderItemStatus.Rejected);
        rejected.Value.Items.Single().RejectionReason.Should().Be(LabSampleRejectionReason.HemolyzedSample);

        var recollectionRequested = await _sut.RequestRecollectionAsync(itemId, actorId: null, CancellationToken.None);
        recollectionRequested.IsSuccess.Should().BeTrue();
        recollectionRequested.Value!.Items.Single().Status.Should().Be(LabOrderItemStatus.RecollectionRequired);

        var recollected = await _sut.CollectSampleAsync(itemId, new CollectSampleRequest { SampleType = LabSampleType.Blood }, actorId: null, CancellationToken.None);
        recollected.IsSuccess.Should().BeTrue();
        recollected.Value!.Items.Single().Status.Should().Be(LabOrderItemStatus.Collected);
        // A fresh collection clears the prior rejection.
        recollected.Value.Items.Single().RejectionReason.Should().BeNull();

        // Every step's own history event was appended.
        recollected.Value.Items.Single().Events.Select(e => e.EventType).Should().Contain(
        [
            LabOrderItemEventType.Created,
            LabOrderItemEventType.SampleCollected,
            LabOrderItemEventType.SampleRejected,
            LabOrderItemEventType.RecollectionRequested,
        ]);
    }

    [Fact]
    public async Task RejectForCorrection_FromPendingVerification_SetsCorrectionRequired_AndCanBeReprocessedAndResubmitted()
    {
        var order = BuildOrder(out var itemId);
        _repository.GetByItemIdAsync(itemId, Arg.Any<CancellationToken>()).Returns(order);

        await _sut.CollectSampleAsync(itemId, new CollectSampleRequest { SampleType = LabSampleType.Blood }, actorId: null, CancellationToken.None);
        await _sut.ReceiveSampleAsync(itemId, actorId: null, CancellationToken.None);
        await _sut.StartProcessingAsync(itemId, actorId: null, CancellationToken.None);
        await _sut.SaveResultDraftAsync(itemId, new SaveResultDraftRequest { Parameters = [new ResultParameterRequest { ParameterName = "Hemoglobin", ResultValue = "13.5" }] }, actorId: null, CancellationToken.None);
        await _sut.SubmitForVerificationAsync(itemId, actorId: null, CancellationToken.None);

        var correctionRequested = await _sut.RejectForCorrectionAsync(itemId, new RejectForCorrectionRequest { Reason = "Value looks implausible, please re-run" }, actorId: null, CancellationToken.None);
        correctionRequested.IsSuccess.Should().BeTrue();
        correctionRequested.Value!.Items.Single().Status.Should().Be(LabOrderItemStatus.CorrectionRequired);

        var reprocessed = await _sut.StartProcessingAsync(itemId, actorId: null, CancellationToken.None);
        reprocessed.IsSuccess.Should().BeTrue();
        reprocessed.Value!.Items.Single().Status.Should().Be(LabOrderItemStatus.Processing);

        var redraft = await _sut.SaveResultDraftAsync(itemId, new SaveResultDraftRequest { Parameters = [new ResultParameterRequest { ParameterName = "Hemoglobin", ResultValue = "13.8" }] }, actorId: null, CancellationToken.None);
        redraft.IsSuccess.Should().BeTrue();

        var resubmitted = await _sut.SubmitForVerificationAsync(itemId, actorId: null, CancellationToken.None);
        resubmitted.IsSuccess.Should().BeTrue();
        resubmitted.Value!.Items.Single().Status.Should().Be(LabOrderItemStatus.PendingVerification);
    }

    [Fact]
    public async Task GenerateReportAsync_WhenNotEveryItemIsVerified_ReturnsNotAllItemsVerifiedFailure()
    {
        var order = BuildOrder(out _);
        _repository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.GenerateReportAsync(order.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(LaboratoryErrorCodes.NotAllItemsVerified);
    }

    [Fact]
    public async Task VerifyAsync_OnAPendingCollectionItem_ReturnsInvalidStatusTransitionFailureNotAnException()
    {
        var order = BuildOrder(out var itemId);
        _repository.GetByItemIdAsync(itemId, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.VerifyAsync(itemId, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(LaboratoryErrorCodes.InvalidStatusTransition);
    }

    [Fact]
    public async Task CreateFromInvoiceAsync_WhenServiceCannotBeResolvedAsLaboratory_ReturnsInvalidServiceOrPackageFailure()
    {
        var invoiceId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        _repository.GetByInvoiceIdAsync(invoiceId, Arg.Any<CancellationToken>()).Returns((LabOrder?)null);
        _diagnosticServiceService.GetByIdAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns(Result<DiagnosticServiceResponse>.Success(new DiagnosticServiceResponse { Id = serviceId, Name = "X-Ray Chest", ServiceType = DiagnosticTestServiceType.Radiology }));

        var result = await _sut.CreateFromInvoiceAsync(ValidRequest(invoiceId, serviceId), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(LaboratoryErrorCodes.InvalidServiceOrPackage);
        await _repository.DidNotReceive().AddAsync(Arg.Any<LabOrder>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ItemMutator_WhenItemDoesNotExist_ReturnsItemNotFoundFailure()
    {
        _repository.GetByItemIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((LabOrder?)null);

        var result = await _sut.CollectSampleAsync(Guid.NewGuid(), new CollectSampleRequest { SampleType = LabSampleType.Blood }, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(LaboratoryErrorCodes.ItemNotFound);
    }
}
