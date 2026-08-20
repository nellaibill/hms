using FluentAssertions;
using HMS.Modules.Pharmacy.Application.Validators;
using HMS.Modules.Pharmacy.Contracts;
using Xunit;

namespace HMS.UnitTests.Modules.Pharmacy.Application.Validators;

public class CreateDispenseCartRequestValidatorTests
{
    private readonly CreateDispenseCartRequestValidator _sut = new();

    private static DispenseCartLineRequest NewLine(Guid? productId = null, Guid? productBatchId = null, decimal quantity = 4m) => new()
    {
        ProductId = productId ?? Guid.NewGuid(),
        ProductBatchId = productBatchId ?? Guid.NewGuid(),
        Quantity = quantity,
    };

    [Fact]
    public void Validate_WithAtLeastOneValidLine_Passes()
    {
        var request = new CreateDispenseCartRequest { PatientId = Guid.NewGuid(), Lines = [NewLine()] };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithNoLines_Fails()
    {
        var request = new CreateDispenseCartRequest { PatientId = Guid.NewGuid(), Lines = [] };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Lines");
    }

    [Fact]
    public void Validate_WithEmptyPatientId_Fails()
    {
        var request = new CreateDispenseCartRequest { PatientId = Guid.Empty, Lines = [NewLine()] };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PatientId");
    }

    [Fact]
    public void Validate_WithDuplicateProductAndBatchAcrossTwoLines_Fails()
    {
        var productId = Guid.NewGuid();
        var productBatchId = Guid.NewGuid();
        var request = new CreateDispenseCartRequest
        {
            PatientId = Guid.NewGuid(),
            Lines = [NewLine(productId, productBatchId), NewLine(productId, productBatchId)],
        };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Lines" && e.ErrorMessage.Contains("more than once"));
    }

    [Fact]
    public void Validate_WithSameProductButDifferentBatches_Passes()
    {
        var productId = Guid.NewGuid();
        var request = new CreateDispenseCartRequest
        {
            PatientId = Guid.NewGuid(),
            Lines = [NewLine(productId, Guid.NewGuid()), NewLine(productId, Guid.NewGuid())],
        };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithALineHavingZeroQuantity_Fails()
    {
        var request = new CreateDispenseCartRequest { PatientId = Guid.NewGuid(), Lines = [NewLine(quantity: 0m)] };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}
