using FluentAssertions;
using FluentValidation.TestHelper;
using HMS.Modules.ActivityLog.Application;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Validators;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Masters.Application;

/// <summary>ADR-154: a lab test's sample type and every Price List item's list order.</summary>
public class PriceListOrderTests
{
    private static readonly Guid CategoryId = Guid.NewGuid();

    private readonly IDiagnosticServiceRepository _repository = Substitute.For<IDiagnosticServiceRepository>();
    private readonly IDiagnosticCategoryService _categoryService = Substitute.For<IDiagnosticCategoryService>();
    private readonly DiagnosticServiceService _sut;

    public PriceListOrderTests()
    {
        _sut = new DiagnosticServiceService(_repository, _categoryService, Substitute.For<IDiagnosticProviderService>(), Substitute.For<IActivityLogService>());
        _categoryService.GetByIdAsync(CategoryId, Arg.Any<CancellationToken>())
            .Returns(Result<DiagnosticCategoryResponse>.Success(new DiagnosticCategoryResponse { Id = CategoryId }));
    }

    private static CreateDiagnosticServiceRequest Haemoglobin() => new()
    {
        Code = "HB",
        Name = "Haemoglobin",
        CategoryId = CategoryId,
        ServiceType = DiagnosticTestServiceType.Laboratory,
        Price = 50m,
        CostPrice = 35m,
        SampleType = "Blood",
        SortOrder = 1,
        IsActive = true,
    };

    [Fact]
    public async Task CreateAsync_LabTest_KeepsSampleTypeAndListOrder()
    {
        var result = await _sut.CreateAsync(Haemoglobin(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SampleType.Should().Be("Blood");
        result.Value.SortOrder.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_ClearsListOrderWhenLeftOut()
    {
        var created = await _sut.CreateAsync(Haemoglobin(), actorId: null, CancellationToken.None);
        var entity = _repository.ReceivedCalls().Select(call => call.GetArguments().FirstOrDefault()).OfType<DiagnosticService>().Single();
        _repository.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>()).Returns(entity);

        var result = await _sut.UpdateAsync(entity.Id, new UpdateDiagnosticServiceRequest
        {
            Code = "HB",
            Name = "Haemoglobin",
            CategoryId = CategoryId,
            ServiceType = DiagnosticTestServiceType.Laboratory,
            Price = 50m,
            CostPrice = 35m,
            SampleType = "Blood",
            IsActive = true,
        }, actorId: null, CancellationToken.None);

        created.Value!.SortOrder.Should().Be(1);
        result.IsSuccess.Should().BeTrue();
        result.Value!.SortOrder.Should().BeNull();
        result.Value.SampleType.Should().Be("Blood");
    }

    [Fact]
    public void Validator_RejectsUnknownSampleType()
    {
        var request = Haemoglobin() with { SampleType = "Saliva" };

        new CreateDiagnosticServiceRequestValidator().TestValidate(request).ShouldHaveValidationErrorFor(x => x.SampleType);
    }

    [Fact]
    public void Validator_RejectsSampleTypeOnRadiology()
    {
        var request = Haemoglobin() with { ServiceType = DiagnosticTestServiceType.Radiology, SampleType = "Blood" };

        new CreateDiagnosticServiceRequestValidator().TestValidate(request).ShouldHaveValidationErrorFor(x => x.SampleType);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(100000)]
    public void Validators_RejectListOrderOutOfRange(int sortOrder)
    {
        new CreateDiagnosticServiceRequestValidator().TestValidate(Haemoglobin() with { SortOrder = sortOrder })
            .ShouldHaveValidationErrorFor(x => x.SortOrder);
        new CreateDiagnosticPackageRequestValidator().TestValidate(new CreateDiagnosticPackageRequest { Code = "CBC", Name = "CBC", TotalPrice = 300m, ServiceIds = [Guid.NewGuid()], SortOrder = sortOrder })
            .ShouldHaveValidationErrorFor(x => x.SortOrder);
        new CreateDiagnosticTestRequestValidator().TestValidate(new CreateDiagnosticTestRequest { Name = "Dressing - Minor", ServiceType = DiagnosticTestServiceType.Procedure, Price = 200m, SortOrder = sortOrder })
            .ShouldHaveValidationErrorFor(x => x.SortOrder);
    }

    [Fact]
    public void Validators_AcceptNoListOrder()
    {
        new CreateDiagnosticServiceRequestValidator().TestValidate(Haemoglobin() with { SortOrder = null, SampleType = null })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void SetListing_RejectsSampleTypeOnRadiology()
    {
        var scan = DiagnosticService.Create("XRAY", "Digital X-ray", CategoryId, DiagnosticTestServiceType.Radiology, false, null, 300m, 100m, true, null);

        var act = () => scan.SetListing("Blood", 1);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetSortOrder_PackageAndTest_KeepTheValue()
    {
        var package = DiagnosticPackage.Create("CBC", "Complete Blood Count (CBC)", null, 300m, 180m, true, [Guid.NewGuid()], null);
        var dressing = DiagnosticTest.Create("Dressing - Minor", DiagnosticTestServiceType.Procedure, "Procedural Charges", 200m, 150m, false, null, true, null);

        package.SetSortOrder(1);
        dressing.SetSortOrder(7);

        package.SortOrder.Should().Be(1);
        dressing.SortOrder.Should().Be(7);
    }
}
