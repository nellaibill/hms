using FluentAssertions;
using HMS.Modules.Pharmacy.Application;
using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using HMS.Modules.Patients.Application;
using HMS.Modules.Products.Application;
using HMS.Modules.Products.Contracts;
using HMS.Shared.Kernel;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;
using PatientResponse = HMS.Modules.Patients.Contracts.PatientResponse;

namespace HMS.UnitTests.Modules.Pharmacy.Application;

public class DispenseServiceTests
{
    private readonly IPharmacyStockBalanceRepository _balanceRepository = Substitute.For<IPharmacyStockBalanceRepository>();
    private readonly IPharmacyStockTransactionRepository _transactionRepository = Substitute.For<IPharmacyStockTransactionRepository>();
    private readonly IProductService _productService = Substitute.For<IProductService>();
    private readonly IProductBatchService _productBatchService = Substitute.For<IProductBatchService>();
    private readonly IPatientService _patientService = Substitute.For<IPatientService>();
    private readonly DispenseService _sut;

    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _productBatchId = Guid.NewGuid();
    private readonly Guid _patientId = Guid.NewGuid();

    public DispenseServiceTests()
    {
        _sut = new DispenseService(_balanceRepository, _transactionRepository, _productService, _productBatchService, _patientService);

        // Happy-path defaults: a valid patient, product, and a non-expired batch with 10 on
        // hand. Each failure-path test overrides the relevant substitute.
        _patientService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<PatientResponse>.Success(new PatientResponse { Uhid = "P-2026-000001", FirstName = "Jane", LastName = "Doe" }));
        _productService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProductResponse>.Success(new ProductResponse { Id = _productId, ProductName = "Paracetamol 500mg" }));
        _productBatchService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProductBatchResponse>.Success(new ProductBatchResponse { Id = _productBatchId, ProductId = _productId, BatchNo = "B-2026-001", ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1) }));

        var balance = PharmacyStockBalance.Create(_productId, _productBatchId, null);
        balance.Receive(10m, null);
        _balanceRepository.GetByProductAndBatchAsync(_productId, _productBatchId, Arg.Any<CancellationToken>()).Returns(balance);
    }

    private CreateDispenseRequest NewRequest() => new()
    {
        ProductId = _productId,
        ProductBatchId = _productBatchId,
        PatientId = _patientId,
        Quantity = 4m,
        Remarks = "Ward dispense",
    };

    [Fact]
    public async Task CreateAsync_WithValidRequest_DecrementsBalanceAndRecordsTransaction()
    {
        var result = await _sut.CreateAsync(NewRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ProductName.Should().Be("Paracetamol 500mg");
        result.Value.BatchNo.Should().Be("B-2026-001");
        result.Value.PatientName.Should().Be("Jane Doe");
        result.Value.Quantity.Should().Be(4m);
        result.Value.BalanceAfter.Should().Be(6m);

        await _transactionRepository.Received(1).AddAsync(Arg.Any<PharmacyStockTransaction>(), Arg.Any<CancellationToken>());
        // Two SaveChangesAsync calls by design: one commits the balance decrement (inside the
        // concurrency retry loop), one commits the ledger insert afterward — kept separate so
        // a retry after a concurrency conflict never re-adds a second ledger row for the same
        // dispense (see DispenseService.CreateAsync's own comments).
        await _balanceRepository.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenPatientDoesNotExist_ReturnsInvalidPatientFailure()
    {
        _patientService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<PatientResponse>.Failure("PATIENTS.NOT_FOUND", "not found"));

        var result = await _sut.CreateAsync(NewRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PharmacyErrorCodes.InvalidPatient);
        await _transactionRepository.DidNotReceive().AddAsync(Arg.Any<PharmacyStockTransaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenProductDoesNotExist_ReturnsInvalidProductFailure()
    {
        _productService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProductResponse>.Failure("PRODUCTS.NOT_FOUND", "not found"));

        var result = await _sut.CreateAsync(NewRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PharmacyErrorCodes.InvalidProduct);
    }

    [Fact]
    public async Task CreateAsync_WhenBatchDoesNotBelongToProduct_ReturnsInvalidBatchFailure()
    {
        _productBatchService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProductBatchResponse>.Failure("PRODUCTS.NOT_FOUND", "batch not found for product"));

        var result = await _sut.CreateAsync(NewRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PharmacyErrorCodes.InvalidBatch);
    }

    [Fact]
    public async Task CreateAsync_WhenBatchHasExpired_ReturnsBatchExpiredFailure()
    {
        _productBatchService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProductBatchResponse>.Success(new ProductBatchResponse
            {
                Id = _productBatchId,
                ProductId = _productId,
                BatchNo = "B-2025-999",
                ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1),
            }));

        var result = await _sut.CreateAsync(NewRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PharmacyErrorCodes.BatchExpired);
        await _transactionRepository.DidNotReceive().AddAsync(Arg.Any<PharmacyStockTransaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenQuantityExceedsBalance_ReturnsInsufficientStockFailure()
    {
        var request = NewRequest() with { Quantity = 999m };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PharmacyErrorCodes.InsufficientStock);
        result.Error.Should().Contain("10");
        await _transactionRepository.DidNotReceive().AddAsync(Arg.Any<PharmacyStockTransaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenNoBalanceRowExists_TreatsQuantityOnHandAsZeroAndReturnsInsufficientStock()
    {
        _balanceRepository.GetByProductAndBatchAsync(_productId, _productBatchId, Arg.Any<CancellationToken>())
            .Returns((PharmacyStockBalance?)null);

        var result = await _sut.CreateAsync(NewRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PharmacyErrorCodes.InsufficientStock);
    }

    [Fact]
    public async Task CreateAsync_WhenFirstSaveThrowsConcurrencyException_RetriesAgainstRefreshedBalanceAndSucceeds()
    {
        // The first read sees 10 on hand (what our caller believes is available). Before our
        // SaveChangesAsync commits, a concurrent dispense wins the race and takes it to 6 —
        // simulated here by the repository returning a second, already-fresher balance
        // instance on the retry's re-fetch.
        var staleBalance = PharmacyStockBalance.Create(_productId, _productBatchId, null);
        staleBalance.Receive(10m, null);

        var refreshedBalance = PharmacyStockBalance.Create(_productId, _productBatchId, null);
        refreshedBalance.Receive(6m, null);

        _balanceRepository.GetByProductAndBatchAsync(_productId, _productBatchId, Arg.Any<CancellationToken>())
            .Returns(staleBalance, refreshedBalance);

        var saveAttempt = 0;
        _balanceRepository.When(x => x.SaveChangesAsync(Arg.Any<CancellationToken>()))
            .Do(_ =>
            {
                saveAttempt++;
                if (saveAttempt == 1)
                {
                    throw new DbUpdateConcurrencyException();
                }
            });

        var request = NewRequest() with { Quantity = 5m };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Re-validated against the refreshed balance (6), not the stale one (10): 6 - 5 = 1.
        result.Value!.BalanceAfter.Should().Be(1m);

        await _balanceRepository.Received(2).GetByProductAndBatchAsync(_productId, _productBatchId, Arg.Any<CancellationToken>());
        // 3 SaveChangesAsync calls: the failed attempt 1, the successful attempt 2 (both
        // committing just the balance), then the separate final commit for the ledger insert.
        await _balanceRepository.Received(3).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _transactionRepository.Received(1).AddAsync(Arg.Any<PharmacyStockTransaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenRetryFindsInsufficientRefreshedStock_ReturnsInsufficientStockWithoutASecondSave()
    {
        var staleBalance = PharmacyStockBalance.Create(_productId, _productBatchId, null);
        staleBalance.Receive(10m, null);

        // The winner of the race dispensed enough that our loser's request no longer fits.
        var refreshedBalance = PharmacyStockBalance.Create(_productId, _productBatchId, null);
        refreshedBalance.Receive(3m, null);

        _balanceRepository.GetByProductAndBatchAsync(_productId, _productBatchId, Arg.Any<CancellationToken>())
            .Returns(staleBalance, refreshedBalance);

        var saveAttempt = 0;
        _balanceRepository.When(x => x.SaveChangesAsync(Arg.Any<CancellationToken>()))
            .Do(_ =>
            {
                saveAttempt++;
                if (saveAttempt == 1)
                {
                    throw new DbUpdateConcurrencyException();
                }
            });

        var request = NewRequest() with { Quantity = 5m };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PharmacyErrorCodes.InsufficientStock);
        await _transactionRepository.DidNotReceive().AddAsync(Arg.Any<PharmacyStockTransaction>(), Arg.Any<CancellationToken>());
    }
}
