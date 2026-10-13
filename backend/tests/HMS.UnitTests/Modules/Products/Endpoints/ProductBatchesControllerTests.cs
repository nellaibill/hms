using System.Security.Claims;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.Products.Application;
using HMS.Modules.Products.Application.Validators;
using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Endpoints;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Products.Endpoints;

// Controller-level for the same reason as ProductsControllerTests: withholding a batch's
// purchase rate (ADR-077's rule, extended to batches) depends on the caller's ClaimsPrincipal.
public class ProductBatchesControllerTests
{
    private readonly IProductBatchService _service = Substitute.For<IProductBatchService>();
    private readonly IValidator<UpdateProductBatchRequest> _updateValidator = Substitute.For<IValidator<UpdateProductBatchRequest>>();
    private readonly ProductBatchesController _sut;

    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _batchId = Guid.NewGuid();

    public ProductBatchesControllerTests()
    {
        _sut = new ProductBatchesController(_service, Substitute.For<IValidator<CreateProductBatchRequest>>(), _updateValidator);
        _updateValidator.ValidateAsync(Arg.Any<UpdateProductBatchRequest>(), Arg.Any<CancellationToken>()).Returns(new ValidationResult());
    }

    private void SetCaller(bool includingViewCost)
    {
        var claims = new List<Claim> { new("Permission", "pharmacy.view"), new("Permission", "pharmacy.edit") };
        if (includingViewCost)
        {
            claims.Add(new Claim("Permission", "pharmacy.view-cost"));
        }

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) },
        };
    }

    private ProductBatchResponse Batch() => new()
    {
        Id = _batchId,
        ProductId = _productId,
        BatchNo = "B-2026-001",
        Mrp = 24.50m,
        PurchaseRate = 17.20m,
        SellingRate = 23m,
    };

    private static ProductBatchResponse Data(IActionResult result) =>
        ((ApiResponse<ProductBatchResponse>)((OkObjectResult)result).Value!).Data!;

    [Fact]
    public async Task GetById_WithholdsPurchaseRateButNotMrpFromACallerWithoutViewCost()
    {
        SetCaller(includingViewCost: false);
        _service.GetByIdAsync(_productId, _batchId, Arg.Any<CancellationToken>()).Returns(Result<ProductBatchResponse>.Success(Batch()));

        var batch = Data(await _sut.GetById(_productId, _batchId, CancellationToken.None));

        batch.PurchaseRate.Should().BeNull();
        batch.Mrp.Should().Be(24.50m);
        batch.SellingRate.Should().Be(23m);
    }

    [Fact]
    public async Task GetById_ReturnsPurchaseRateToACallerWithViewCost()
    {
        SetCaller(includingViewCost: true);
        _service.GetByIdAsync(_productId, _batchId, Arg.Any<CancellationToken>()).Returns(Result<ProductBatchResponse>.Success(Batch()));

        var batch = Data(await _sut.GetById(_productId, _batchId, CancellationToken.None));

        batch.PurchaseRate.Should().Be(17.20m);
    }

    [Fact]
    public async Task GetPaged_WithholdsPurchaseRateOnEveryBatch()
    {
        SetCaller(includingViewCost: false);
        _service.GetPagedAsync(_productId, Arg.Any<ProductBatchListQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProductBatchResponse>([Batch(), Batch()], 1, 20, 2));

        var result = (OkObjectResult)await _sut.GetPaged(_productId, new ProductBatchListQuery(), CancellationToken.None);

        var batches = ((ApiResponse<IReadOnlyList<ProductBatchResponse>>)result.Value!).Data!;
        batches.Should().OnlyContain(b => b.PurchaseRate == null && b.Mrp == 24.50m);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Update_KeepsTheStoredPurchaseRateOnlyWhenTheCallerCouldNotSeeIt(bool includingViewCost, bool expectedKeepPurchaseRate)
    {
        // A caller without view-cost is shown a withheld (null) rate; saving that form back
        // must not overwrite the real rate with the null it was shown.
        SetCaller(includingViewCost);
        _service.UpdateAsync(_productId, _batchId, Arg.Any<UpdateProductBatchRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(Result<ProductBatchResponse>.Success(Batch()));

        await _sut.Update(_productId, _batchId, new UpdateProductBatchRequest(), CancellationToken.None);

        await _service.Received(1).UpdateAsync(
            _productId, _batchId, Arg.Any<UpdateProductBatchRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>(), expectedKeepPurchaseRate);
    }
}

public class ProductBatchValidatorTests
{
    private readonly CreateProductBatchRequestValidator _sut = new();

    private static CreateProductBatchRequest Request(decimal? mrp = null, decimal? purchaseRate = null, decimal? sellingRate = null) => new()
    {
        BatchNo = "B-2026-001",
        ManufactureDate = new DateOnly(2026, 1, 1),
        ExpiryDate = new DateOnly(2028, 1, 1),
        Mrp = mrp,
        PurchaseRate = purchaseRate,
        SellingRate = sellingRate,
    };

    [Fact]
    public void ABatchWithNoPricesIsValid()
    {
        _sut.Validate(Request()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ASellingRateAboveMrpIsRejected()
    {
        var result = _sut.Validate(Request(mrp: 24.50m, sellingRate: 25m));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductBatchRequest.SellingRate));
    }

    [Fact]
    public void ASellingRateAtMrpIsValid()
    {
        _sut.Validate(Request(mrp: 24.50m, sellingRate: 24.50m)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void NegativePricesAreRejected()
    {
        _sut.Validate(Request(mrp: -1m)).IsValid.Should().BeFalse();
        _sut.Validate(Request(purchaseRate: -1m)).IsValid.Should().BeFalse();
        _sut.Validate(Request(sellingRate: -1m)).IsValid.Should().BeFalse();
    }
}
