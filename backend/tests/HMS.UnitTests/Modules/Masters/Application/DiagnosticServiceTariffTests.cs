using HMS.Modules.ActivityLog.Application;
using FluentAssertions;
using FluentValidation.TestHelper;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Validators;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Masters.Application;

/// <summary>ADR-140: a Radiology service's doctor's share, bedside add-on and rate options.</summary>
public class DiagnosticServiceTariffTests
{
    private static readonly Guid CategoryId = Guid.NewGuid();

    private readonly IDiagnosticServiceRepository _repository = Substitute.For<IDiagnosticServiceRepository>();
    private readonly IDiagnosticCategoryService _categoryService = Substitute.For<IDiagnosticCategoryService>();
    private readonly IDiagnosticProviderService _providerService = Substitute.For<IDiagnosticProviderService>();
    private readonly DiagnosticServiceService _sut;

    public DiagnosticServiceTariffTests()
    {
        _sut = new DiagnosticServiceService(_repository, _categoryService, _providerService, Substitute.For<IActivityLogService>());
        _categoryService.GetByIdAsync(CategoryId, Arg.Any<CancellationToken>())
            .Returns(Result<DiagnosticCategoryResponse>.Success(new DiagnosticCategoryResponse { Id = CategoryId }));
    }

    private static CreateDiagnosticServiceRequest GrowthScan() => new()
    {
        Code = "ANCGROWTHSCAN",
        Name = "ANC - Growth Scan",
        CategoryId = CategoryId,
        ServiceType = DiagnosticTestServiceType.Radiology,
        Price = 2200m,
        DoctorCharge = 800m,
        CostPrice = 200m,
        BedsidePrice = 500m,
        BedsideDoctorCharge = 200m,
        RateOptions =
        [
            new DiagnosticServiceRateOptionDto { Name = "LH", Price = 1300m, DoctorCharge = 0m, CostPrice = 200m },
            new DiagnosticServiceRateOptionDto { Name = "MR", Price = 1300m, DoctorCharge = 1300m, CostPrice = 0m },
        ],
        IsActive = true,
    };

    private static UpdateDiagnosticServiceRequest ToUpdate(CreateDiagnosticServiceRequest request, IReadOnlyList<DiagnosticServiceRateOptionDto> options) => new()
    {
        Code = request.Code,
        Name = request.Name,
        CategoryId = request.CategoryId,
        ServiceType = request.ServiceType,
        Price = request.Price,
        DoctorCharge = request.DoctorCharge,
        CostPrice = request.CostPrice,
        BedsidePrice = request.BedsidePrice,
        BedsideDoctorCharge = request.BedsideDoctorCharge,
        RateOptions = options,
        IsActive = true,
    };

    [Fact]
    public async Task CreateAsync_Radiology_KeepsDoctorShareBedsideAndRateOptionsInOrder()
    {
        var result = await _sut.CreateAsync(GrowthScan(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var service = result.Value!;
        service.DoctorCharge.Should().Be(800m);
        service.CostPrice.Should().Be(200m);
        service.BedsidePrice.Should().Be(500m);
        service.BedsideDoctorCharge.Should().Be(200m);
        service.RateOptions.Select(o => o.Name).Should().Equal("LH", "MR");
        service.RateOptions.Should().OnlyContain(o => o.Id.HasValue);
        service.RateOptions[1].DoctorCharge.Should().Be(1300m);
    }

    [Fact]
    public async Task UpdateAsync_KeepsAnExistingOptionsId_AddsNewOnes_AndRemovesOnesLeftOut()
    {
        var created = await _sut.CreateAsync(GrowthScan(), actorId: null, CancellationToken.None);
        var entity = _repository.ReceivedCalls()
            .Select(call => call.GetArguments().FirstOrDefault())
            .OfType<DiagnosticService>()
            .Single();
        _repository.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>()).Returns(entity);
        var lh = created.Value!.RateOptions.Single(o => o.Name == "LH");

        var result = await _sut.UpdateAsync(
            entity.Id,
            ToUpdate(GrowthScan(),
            [
                lh with { Price = 1400m },
                new DiagnosticServiceRateOptionDto { Name = "Night", Price = 2500m, DoctorCharge = 900m, CostPrice = 200m },
            ]),
            actorId: null,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RateOptions.Select(o => o.Name).Should().Equal("LH", "Night");
        result.Value.RateOptions[0].Id.Should().Be(lh.Id, "a bill line that points at LH must still find it");
        result.Value.RateOptions[0].Price.Should().Be(1400m);
    }

    [Fact]
    public void SetTariff_OnALabTest_RejectsADoctorsShare()
    {
        var labTest = DiagnosticService.Create("CBC", "CBC", CategoryId, DiagnosticTestServiceType.Laboratory, false, null, 300m, 180m, true, null);

        var act = () => labTest.SetTariff(50m, null, null, []);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetTariff_RejectsHalfABedsideAddOn()
    {
        var scan = DiagnosticService.Create("USG", "Breast", CategoryId, DiagnosticTestServiceType.Radiology, false, null, 2000m, 200m, true, null);

        var act = () => scan.SetTariff(1000m, 500m, null, []);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Validator_LabTestWithDoctorShareRateOptionsOrBedside_HasErrors()
    {
        var validator = new CreateDiagnosticServiceRequestValidator();
        var request = GrowthScan() with { ServiceType = DiagnosticTestServiceType.Laboratory };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.DoctorCharge);
        result.ShouldHaveValidationErrorFor(x => x.RateOptions);
        result.ShouldHaveValidationErrorFor(x => x.BedsidePrice);
    }

    [Fact]
    public void Validator_ValidRadiologyTariff_HasNoErrors()
    {
        var validator = new UpdateDiagnosticServiceRequestValidator();

        var result = validator.TestValidate(ToUpdate(GrowthScan(), GrowthScan().RateOptions));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_DuplicateOptionNamesOrHalfBedside_HasErrors()
    {
        var validator = new CreateDiagnosticServiceRequestValidator();
        var request = GrowthScan() with
        {
            BedsideDoctorCharge = null,
            RateOptions =
            [
                new DiagnosticServiceRateOptionDto { Name = "LH", Price = 1300m },
                new DiagnosticServiceRateOptionDto { Name = "lh ", Price = 1300m },
            ],
        };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.RateOptions).WithErrorMessage("Each rate option needs a different name.");
        result.ShouldHaveValidationErrorFor(x => x.BedsidePrice);
    }

    [Fact]
    public void Validator_RateOptionWithoutNameOrNegativeAmount_HasErrors()
    {
        var validator = new CreateDiagnosticServiceRequestValidator();
        var request = GrowthScan() with
        {
            RateOptions = [new DiagnosticServiceRateOptionDto { Name = "", Price = -1m }],
        };

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "A rate option needs a name.");
        result.Errors.Should().Contain(e => e.ErrorMessage == "A rate option's price cannot be negative.");
    }
}
