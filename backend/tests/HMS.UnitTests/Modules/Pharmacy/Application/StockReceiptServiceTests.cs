using FluentAssertions;
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

public class StockReceiptServiceTests
{
    private readonly IPharmacyStockBalanceRepository _balanceRepository = Substitute.For<IPharmacyStockBalanceRepository>();
    private readonly IPharmacyStockTransactionRepository _transactionRepository = Substitute.For<IPharmacyStockTransactionRepository>();
    private readonly IProductService _productService = Substitute.For<IProductService>();
    private readonly IProductBatchService _productBatchService = Substitute.For<IProductBatchService>();
    private readonly StockReceiptService _sut;

    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _productBatchId = Guid.NewGuid();

    public StockReceiptServiceTests()
    {
        _sut = new StockReceiptService(_balanceRepository, _transactionRepository, _productService, _productBatchService);

        // Happy-path defaults: a valid product and batch, no existing balance row yet. Each
        // failure-path test overrides the relevant substitute.
        _productService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProductResponse>.Success(new ProductResponse { Id = _productId, ProductName = "Paracetamol 500mg" }));
        _productBatchService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProductBatchResponse>.Success(new ProductBatchResponse { Id = _productBatchId, ProductId = _productId, BatchNo = "B-2026-001", ExpiryDate = new DateOnly(2027, 1, 1) }));
        _balanceRepository.GetByProductAndBatchAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((PharmacyStockBalance?)null);
    }

    private CreateStockReceiptRequest NewRequest() => new()
    {
        ProductId = _productId,
        ProductBatchId = _productBatchId,
        Quantity = 100m,
        Remarks = "Initial stock",
    };

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesBalanceAndTransaction()
    {
        var result = await _sut.CreateAsync(NewRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ProductName.Should().Be("Paracetamol 500mg");
        result.Value.BatchNo.Should().Be("B-2026-001");
        result.Value.Quantity.Should().Be(100m);
        result.Value.BalanceAfter.Should().Be(100m);

        await _balanceRepository.Received(1).AddAsync(Arg.Any<PharmacyStockBalance>(), Arg.Any<CancellationToken>());
        await _transactionRepository.Received(1).AddAsync(Arg.Any<PharmacyStockTransaction>(), Arg.Any<CancellationToken>());
        await _balanceRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithExistingBalance_AddsToExistingQuantityInsteadOfCreatingNewRow()
    {
        var existing = PharmacyStockBalance.Create(_productId, _productBatchId, null);
        existing.Receive(50m, null);
        _balanceRepository.GetByProductAndBatchAsync(_productId, _productBatchId, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _sut.CreateAsync(NewRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.BalanceAfter.Should().Be(150m);
        await _balanceRepository.DidNotReceive().AddAsync(Arg.Any<PharmacyStockBalance>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenProductDoesNotExist_ReturnsInvalidProductFailure()
    {
        _productService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProductResponse>.Failure("PRODUCTS.NOT_FOUND", "not found"));

        var result = await _sut.CreateAsync(NewRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PharmacyErrorCodes.InvalidProduct);
        await _transactionRepository.DidNotReceive().AddAsync(Arg.Any<PharmacyStockTransaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenBatchDoesNotBelongToProduct_ReturnsInvalidBatchFailure()
    {
        _productBatchService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProductBatchResponse>.Failure("PRODUCTS.NOT_FOUND", "batch not found for product"));

        var result = await _sut.CreateAsync(NewRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PharmacyErrorCodes.InvalidBatch);
        await _transactionRepository.DidNotReceive().AddAsync(Arg.Any<PharmacyStockTransaction>(), Arg.Any<CancellationToken>());
    }
}
