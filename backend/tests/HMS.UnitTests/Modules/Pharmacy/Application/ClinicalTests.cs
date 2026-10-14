using FluentAssertions;
using HMS.Modules.IPD.Application;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.OpdConsultation.Application;
using HMS.Modules.OpdConsultation.Contracts;
using HMS.Modules.Pharmacy.Application;
using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using HMS.Modules.Products.Application;
using HMS.Modules.Products.Contracts;
using HMS.Shared.Kernel;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Pharmacy.Application;

public class PrescriptionQuantityTests
{
    [Theory]
    [InlineData("1-0-1", 5, 10)]
    [InlineData("1-1-1", 3, 9)]
    [InlineData("1 - 0 - 0", 30, 30)]
    [InlineData("½-0-½", 10, 10)]
    [InlineData("0.5-0-0.5", 7, 7)]
    [InlineData("1-1-1-1", 2, 8)]
    [InlineData("OD", 7, 7)]
    [InlineData("bd", 5, 10)]
    [InlineData("T.D.S", 5, 15)]
    [InlineData("QID", 3, 12)]
    [InlineData("HS", 10, 10)]
    [InlineData("Twice daily", 5, 10)]
    public void SuggestQuantity_ReadsTheWaysAFrequencyIsWritten(string frequency, int days, decimal expected)
    {
        PrescriptionQueueService.SuggestQuantity(frequency, days).Should().Be(expected);
    }

    [Theory]
    [InlineData("SOS", 5)]
    [InlineData("as directed", 5)]
    [InlineData("1-0-1", null)]
    [InlineData(null, 5)]
    [InlineData("BD", 0)]
    public void SuggestQuantity_WhenItCannotTell_SaysNothing(string? frequency, int? days)
    {
        PrescriptionQueueService.SuggestQuantity(frequency, days).Should().BeNull();
    }

    [Fact]
    public void SuggestQuantity_RoundsAPartDoseUpToAWholeUnit()
    {
        // Half a tablet at night for 5 days is 2.5 tablets: three have to be given.
        PrescriptionQueueService.SuggestQuantity("0-0-½", 5).Should().Be(3m);
    }
}

public class PrescriptionQueueServiceTests
{
    [Fact]
    public async Task GetQueueAsync_SetsEachPrescriptionAgainstItsBills()
    {
        var opd = Substitute.For<IOpdConsultationService>();
        var sales = Substitute.For<IPharmacySaleRepository>();
        var waiting = Guid.NewGuid();
        var billed = Guid.NewGuid();

        opd.GetPrescriptionQueueAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(
        [
            new OpdPrescriptionQueueItemResponse
            {
                ConsultationId = waiting,
                PatientName = "Kala R",
                ConsultantName = "Dr. S. Karthikeyan",
                Prescriptions = [new OpdConsultationPrescriptionResponse { DrugName = "Dolo 650", Frequency = "1-0-1", DurationDays = 5 }],
            },
            new OpdPrescriptionQueueItemResponse { ConsultationId = billed, PatientName = "Arun M", Prescriptions = [new OpdConsultationPrescriptionResponse { DrugName = "Pan 40", Frequency = "SOS" }] },
        ]);
        sales.GetBillsForPrescriptionsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, IReadOnlyList<PrescriptionBillResponse>> { [billed] = [new PrescriptionBillResponse { BillNo = "PH-2026-000007", NetAmount = 158m }] });

        var queue = await new PrescriptionQueueService(opd, sales).GetQueueAsync(new DateOnly(2026, 10, 14), new DateOnly(2026, 10, 14), CancellationToken.None);

        queue.Should().HaveCount(2);
        queue[0].Bills.Should().BeEmpty();
        queue[0].DoctorName.Should().Be("Dr. S. Karthikeyan");
        queue[0].Drugs.Single().SuggestedQuantity.Should().Be(10m);
        queue[1].Bills.Single().BillNo.Should().Be("PH-2026-000007");
        queue[1].Drugs.Single().SuggestedQuantity.Should().BeNull();
    }

    [Fact]
    public async Task GetQueueAsync_AsksForTheWholeOfTheLastDay()
    {
        var opd = Substitute.For<IOpdConsultationService>();
        opd.GetPrescriptionQueueAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns([]);
        var sales = Substitute.For<IPharmacySaleRepository>();
        sales.GetBillsForPrescriptionsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, IReadOnlyList<PrescriptionBillResponse>>());

        await new PrescriptionQueueService(opd, sales).GetQueueAsync(new DateOnly(2026, 10, 14), new DateOnly(2026, 10, 14), CancellationToken.None);

        // 14 Oct in India runs from 18:30 UTC on the 13th to 18:30 UTC on the 14th.
        await opd.Received(1).GetPrescriptionQueueAsync(
            new DateTime(2026, 10, 13, 18, 30, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 14, 18, 30, 0, DateTimeKind.Utc),
            Arg.Any<CancellationToken>());
    }
}

public class IndentDomainTests
{
    private static PharmacyIndent NewIndent()
        => PharmacyIndent.Create("IND-2026-000001", Guid.NewGuid(), Guid.NewGuid(), "ADM-2026-000006", Guid.NewGuid(), "Meharaj Kumar", "P-2026-000005", "Ward A · 12", "Dr. Bala", false, null, null);

    [Fact]
    public void RecordIssue_OfPartOfIt_LeavesTheIndentPartlyIssued()
    {
        var indent = NewIndent();
        var pan = Guid.NewGuid();
        var dolo = Guid.NewGuid();
        indent.AddLine(pan, "Pan 40", 30m, null);
        indent.AddLine(dolo, "Dolo 650", 10m, "SOS for fever");

        indent.RecordIssue(pan, 30m, null);

        indent.Status.Should().Be(IndentStatus.PartlyIssued);
        indent.Lines[1].PendingQuantity.Should().Be(10m);
    }

    [Fact]
    public void RecordIssue_OfAllOfIt_ClosesTheIndent()
    {
        var indent = NewIndent();
        var pan = Guid.NewGuid();
        indent.AddLine(pan, "Pan 40", 30m, null);

        indent.RecordIssue(pan, 15m, null);
        indent.RecordIssue(pan, 15m, null);

        indent.Status.Should().Be(IndentStatus.Issued);
        indent.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void RecordIssue_OfSomethingNotAskedFor_ChangesNothing()
    {
        var indent = NewIndent();
        indent.AddLine(Guid.NewGuid(), "Pan 40", 30m, null);

        indent.RecordIssue(Guid.NewGuid(), 5m, null);

        indent.Status.Should().Be(IndentStatus.Pending);
    }

    [Fact]
    public void Cancel_APartIssuedIndent_FinishesItRatherThanCancellingIt()
    {
        var indent = NewIndent();
        var pan = Guid.NewGuid();
        indent.AddLine(pan, "Pan 40", 30m, null);
        indent.AddLine(Guid.NewGuid(), "Dolo 650", 10m, null);
        indent.RecordIssue(pan, 30m, null);

        indent.Cancel(null);

        indent.Status.Should().Be(IndentStatus.Issued, "what was issued stands");
    }

    [Fact]
    public void Cancel_AnUntouchedIndent_CancelsIt()
    {
        var indent = NewIndent();
        indent.AddLine(Guid.NewGuid(), "Pan 40", 30m, null);

        indent.Cancel(null);

        indent.Status.Should().Be(IndentStatus.Cancelled);
    }
}

public class IndentServiceTests
{
    private readonly IPharmacyIndentRepository _repository = Substitute.For<IPharmacyIndentRepository>();
    private readonly IPharmacyStoreRepository _storeRepository = Substitute.For<IPharmacyStoreRepository>();
    private readonly FakePharmacyUnitOfWork _unitOfWork = new();
    private readonly IProductService _productService = Substitute.For<IProductService>();
    private readonly IAdmissionService _admissionService = Substitute.For<IAdmissionService>();
    private readonly IndentService _sut;
    private readonly Guid _admissionId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();

    public IndentServiceTests()
    {
        StockFixture.DefaultStore(_storeRepository);
        _sut = new IndentService(_repository, _storeRepository, _unitOfWork, _productService, _admissionService);

        _productService.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([new ProductResponse { Id = _productId, ProductName = "Pan 40", IsActive = true }]);
        Admission(AdmissionStatus.Admitted);
    }

    private void Admission(AdmissionStatus status) => _admissionService.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>())
        .Returns(Result<AdmissionResponse>.Success(new AdmissionResponse
        {
            Id = _admissionId,
            AdmissionNumber = "ADM-2026-000006",
            PatientId = Guid.NewGuid(),
            PatientName = "Meharaj Kumar",
            Uhid = "P-2026-000005",
            WardName = "Ward A",
            BedNumber = "12",
            ConsultantName = "Dr. Bala",
            Status = status,
        }));

    private CreateIndentRequest Request() => new()
    {
        AdmissionId = _admissionId,
        IsUrgent = true,
        Lines = [new IndentLineRequest { ProductId = _productId, Quantity = 30m, Instructions = "1-0-1" }],
    };

    [Fact]
    public async Task CreateAsync_TakesThePatientWardAndDoctorFromTheAdmission()
    {
        var result = await _sut.CreateAsync(Request(), null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var indent = result.Value!;
        indent.IndentNo.Should().Be("IND-2026-000001");
        indent.PatientName.Should().Be("Meharaj Kumar");
        indent.Location.Should().Be("Ward A · 12");
        indent.DoctorName.Should().Be("Dr. Bala");
        indent.Status.Should().Be(IndentStatus.Pending);
        indent.IsUrgent.Should().BeTrue();
        indent.Lines.Single().PendingQuantity.Should().Be(30m);
    }

    [Fact]
    public async Task CreateAsync_ForAPatientNoLongerAdmitted_IsRefused()
    {
        Admission(AdmissionStatus.Discharged);

        var result = await _sut.CreateAsync(Request(), null, CancellationToken.None);

        result.ErrorCode.Should().Be("PHARMACY.INVALID_ADMISSION");
        _unitOfWork.Saves.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_WithAnUnknownProduct_NamesTheLine()
    {
        var request = Request() with { Lines = [new IndentLineRequest { ProductId = Guid.NewGuid(), Quantity = 1m }] };

        var result = await _sut.CreateAsync(request, null, CancellationToken.None);

        result.ErrorCode.Should().Be("PHARMACY.INVALID_PRODUCT");
        result.Error.Should().StartWith("Line 1:");
    }
}
