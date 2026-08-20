using FluentAssertions;
using HMS.Modules.Billing.Application;
using HMS.Modules.Billing.Contracts;
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
    private readonly IInvoiceService _invoiceService = Substitute.For<IInvoiceService>();
    private readonly DispenseService _sut;

    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _productBatchId = Guid.NewGuid();
    private readonly Guid _patientId = Guid.NewGuid();

    public DispenseServiceTests()
    {
        _sut = new DispenseService(_balanceRepository, _transactionRepository, _productService, _productBatchService, _patientService, _invoiceService);

        // Happy-path defaults: a valid patient, product, and a non-expired batch with 10 on
        // hand. Each failure-path test overrides the relevant substitute.
        _patientService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<PatientResponse>.Success(new PatientResponse { Uhid = "P-2026-000001", FirstName = "Jane", LastName = "Doe" }));
        _productService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProductResponse>.Success(new ProductResponse { Id = _productId, ProductName = "Paracetamol 500mg", SellingPrice = 20m }));
        _productBatchService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProductBatchResponse>.Success(new ProductBatchResponse { Id = _productBatchId, ProductId = _productId, BatchNo = "B-2026-001", ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1) }));
        // Billing succeeds by default — the specific billing-failure/exception tests below
        // override this to exercise DispenseService's best-effort handling (ADR-028).
        _invoiceService.CreateAsync(Arg.Any<CreateInvoiceRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Result<InvoiceResponse>.Success(new InvoiceResponse { Id = Guid.NewGuid(), InvoiceNumber = "INV-2026-000001" }));

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
        // A single SaveChangesAsync commits the balance decrement and the ledger insert
        // together, atomically — never a decremented balance with no matching history row.
        // A second commits the invoice-id link once billing succeeds (see the billing tests
        // below) — the default happy-path mock always succeeds, so that's 2 total here.
        await _balanceRepository.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
        _transactionRepository.DidNotReceive().Detach(Arg.Any<PharmacyStockTransaction>());
    }

    [Fact]
    public async Task CreateAsync_WhenBillingSucceeds_PopulatesInvoiceDetailsAndBuildsACorrectInvoiceRequest()
    {
        var invoiceId = Guid.NewGuid();
        _invoiceService.CreateAsync(Arg.Any<CreateInvoiceRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Result<InvoiceResponse>.Success(new InvoiceResponse { Id = invoiceId, InvoiceNumber = "INV-2026-000042" }));

        var result = await _sut.CreateAsync(NewRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.InvoiceNumber.Should().Be("INV-2026-000042");
        result.Value.InvoiceId.Should().Be(invoiceId);
        result.Value.BillingFailed.Should().BeFalse();
        result.Value.BillingError.Should().BeNull();

        await _invoiceService.Received(1).CreateAsync(
            Arg.Is<CreateInvoiceRequest>(r =>
                r.PatientId == _patientId
                && r.PatientName == "Jane Doe"
                && r.PatientUhid == "P-2026-000001"
                // No CurrentRegistration on the mocked patient, so VisitId falls back to
                // PatientId — mirrors the frontend's own documented fallback.
                && r.VisitId == _patientId
                && r.Items.Count == 1
                && r.Items[0].BillingType == BillingType.Pharmacy
                && r.Items[0].Quantity == 1
                // Quantity is fixed at 1 (CreateInvoiceLineItemRequest.Quantity is int) —
                // the real decimal quantity is priced into UnitPrice as this line's total.
                && r.Items[0].UnitPrice == 4m * 20m),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenBillingFails_StillSucceedsButReportsTheBillingFailure()
    {
        _invoiceService.CreateAsync(Arg.Any<CreateInvoiceRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Result<InvoiceResponse>.Failure("BILLING.SOME_ERROR", "Billing is temporarily unavailable."));

        var result = await _sut.CreateAsync(NewRequest(), actorId: null, CancellationToken.None);

        // The dispense itself — medicine already left the pharmacy, stock already correctly
        // decremented — must never fail just because the separate Billing write did.
        result.IsSuccess.Should().BeTrue();
        result.Value!.BalanceAfter.Should().Be(6m);
        result.Value.BillingFailed.Should().BeTrue();
        result.Value.BillingError.Should().Be("Billing is temporarily unavailable.");
        result.Value.InvoiceId.Should().BeNull();
        result.Value.InvoiceNumber.Should().BeNull();

        // Only the one SaveChangesAsync for the dispense itself — no second save attempting
        // to persist an InvoiceId that was never obtained.
        await _balanceRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenBillingThrows_StillSucceedsButReportsTheBillingFailure()
    {
        _invoiceService.CreateAsync(Arg.Any<CreateInvoiceRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns<Task<Result<InvoiceResponse>>>(_ => throw new InvalidOperationException("Billing DB is unreachable."));

        var result = await _sut.CreateAsync(NewRequest(), actorId: null, CancellationToken.None);

        // A genuinely unexpected exception from Billing (not just a Result.Failure) must not
        // propagate as a 500 on an otherwise-successful dispense either.
        result.IsSuccess.Should().BeTrue();
        result.Value!.BillingFailed.Should().BeTrue();
        result.Value.BillingError.Should().Be("Billing DB is unreachable.");
        result.Value.InvoiceId.Should().BeNull();
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
        // 3 SaveChangesAsync calls: the failed attempt 1 (balance decrement + candidate ledger
        // row together, rolled back), the successful attempt 2 (fresh balance + a fresh ledger
        // row, committed together), and the billing follow-up's InvoiceId link (default mock
        // billing succeeds).
        await _balanceRepository.Received(3).SaveChangesAsync(Arg.Any<CancellationToken>());
        // One AddAsync per attempt: attempt 1's candidate row is detached after the conflict,
        // attempt 2 adds a fresh one that actually persists.
        await _transactionRepository.Received(2).AddAsync(Arg.Any<PharmacyStockTransaction>(), Arg.Any<CancellationToken>());
        _transactionRepository.Received(1).Detach(Arg.Any<PharmacyStockTransaction>());
    }

    [Fact]
    public async Task CreateAsync_WhenRetryFindsInsufficientRefreshedStock_ReturnsInsufficientStockAndDetachesTheAbandonedTransaction()
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
        // Attempt 1 added a candidate row before its SaveChangesAsync hit the concurrency
        // conflict; it must be detached so nothing lingers half-tracked once attempt 2 fails
        // the (now-refreshed) quantity check and returns without adding a replacement.
        await _transactionRepository.Received(1).AddAsync(Arg.Any<PharmacyStockTransaction>(), Arg.Any<CancellationToken>());
        _transactionRepository.Received(1).Detach(Arg.Any<PharmacyStockTransaction>());
    }
}
