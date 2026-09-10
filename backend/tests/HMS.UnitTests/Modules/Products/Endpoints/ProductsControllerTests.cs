using System.Security.Claims;
using FluentAssertions;
using FluentValidation;
using HMS.Modules.Products.Application;
using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Endpoints;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Products.Endpoints;

// The only controller-level test in this suite so far — deliberately, because the redaction
// logic under test (ADR-077) lives in ProductsController itself (it needs the caller's
// ClaimsPrincipal, which a Service-layer test has no access to), not in a place any
// Service-level test could reach. HMS.Modules.Products has no InternalsVisibleTo wiring and
// therefore no way to unit-test its internal Service/Repository layer at all — not needed
// here, since ProductsController/IProductService/ProductResponse are all public.
public class ProductsControllerTests
{
    private readonly IProductService _service = Substitute.For<IProductService>();
    private readonly ProductsController _sut;

    public ProductsControllerTests()
    {
        _sut = new ProductsController(_service, Substitute.For<IValidator<CreateProductRequest>>(), Substitute.For<IValidator<UpdateProductRequest>>());
    }

    [Fact]
    public async Task GetById_RedactsCostPriceForACallerWithoutViewCostPermission()
    {
        SetCaller(includingViewCost: false);
        var productId = Guid.NewGuid();
        _service.GetByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(Result<ProductResponse>.Success(BuildResponse(costPrice: 42.50m)));

        var result = await _sut.GetById(productId, CancellationToken.None);

        var response = ExtractData((OkObjectResult)result);
        response.CostPrice.Should().Be(0);
    }

    [Fact]
    public async Task GetById_ReturnsRealCostPriceForACallerWithViewCostPermission()
    {
        SetCaller(includingViewCost: true);
        var productId = Guid.NewGuid();
        _service.GetByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(Result<ProductResponse>.Success(BuildResponse(costPrice: 42.50m)));

        var result = await _sut.GetById(productId, CancellationToken.None);

        var response = ExtractData((OkObjectResult)result);
        response.CostPrice.Should().Be(42.50m);
    }

    [Fact]
    public async Task GetPaged_RedactsCostPriceOnEveryItemForACallerWithoutViewCostPermission()
    {
        SetCaller(includingViewCost: false);
        var items = new[] { BuildResponse(costPrice: 10m), BuildResponse(costPrice: 20m) };
        _service.GetPagedAsync(Arg.Any<ProductListQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProductResponse>(items, page: 1, pageSize: 20, totalCount: 2));

        var result = await _sut.GetPaged(new ProductListQuery(), CancellationToken.None);

        var envelope = (ApiResponse<IReadOnlyList<ProductResponse>>)((OkObjectResult)result).Value!;
        envelope.Data.Should().OnlyContain(p => p.CostPrice == 0);
    }

    private void SetCaller(bool includingViewCost)
    {
        var claims = new List<Claim> { new("UserId", Guid.NewGuid().ToString()) };
        if (includingViewCost)
        {
            claims.Add(new Claim("Permission", "pharmacy.view-cost"));
        }

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) },
        };
    }

    private static ProductResponse BuildResponse(decimal costPrice) => new()
    {
        Id = Guid.NewGuid(),
        Sku = "SKU-1",
        ProductCode = "P-1",
        ProductName = "Test Product",
        CostPrice = costPrice,
    };

    private static ProductResponse ExtractData(OkObjectResult okResult) =>
        ((ApiResponse<ProductResponse>)okResult.Value!).Data!;
}
