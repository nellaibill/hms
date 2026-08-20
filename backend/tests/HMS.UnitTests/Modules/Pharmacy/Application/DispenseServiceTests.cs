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

    // ---- CreateCartAsync ------------------------------------------------------------------

    private CreateDispenseCartRequest NewCartRequest(params DispenseCartLineRequest[] lines) => new()
    {
        PatientId = _patientId,
        Lines = lines,
    };

    private DispenseCartLineRequest NewLine(Guid productId, Guid productBatchId, decimal quantity) => new()
    {
        ProductId = productId,
        ProductBatchId = productBatchId,
        Quantity = quantity,
        Remarks = "Ward dispense",
    };

    [Fact]
    public async Task CreateCartAsync_WithValidLines_DispensesAllAndBillsOneInvoiceWithNItems()
    {
        var secondProductId = Guid.NewGuid();
        var secondBatchId = Guid.NewGuid();
        _productService.GetByIdAsync(secondProductId, Arg.Any<CancellationToken>())
            .Returns(Result<ProductResponse>.Success(new ProductResponse { Id = secondProductId, ProductName = "Amoxicillin 250mg", SellingPrice = 5m }));
        _productBatchService.GetByIdAsync(secondProductId, secondBatchId, Arg.Any<CancellationToken>())
            .Returns(Result<ProductBatchResponse>.Success(new ProductBatchResponse { Id = secondBatchId, ProductId = secondProductId, BatchNo = "B-2026-777", ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1) }));
        var secondBalance = PharmacyStockBalance.Create(secondProductId, secondBatchId, null);
        secondBalance.Receive(20m, null);
        _balanceRepository.GetByProductAndBatchAsync(secondProductId, secondBatchId, Arg.Any<CancellationToken>()).Returns(secondBalance);

        var invoiceId = Guid.NewGuid();
        _invoiceService.CreateAsync(Arg.Any<CreateInvoiceRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Result<InvoiceResponse>.Success(new InvoiceResponse { Id = invoiceId, InvoiceNumber = "INV-2026-000099" }));

        var request = NewCartRequest(NewLine(_productId, _productBatchId, 4m), NewLine(secondProductId, secondBatchId, 3m));

        var result = await _sut.CreateCartAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Lines.Should().HaveCount(2);
        result.Value.Lines[0].BalanceAfter.Should().Be(6m);
        result.Value.Lines[1].BalanceAfter.Should().Be(17m);
        result.Value.InvoiceId.Should().Be(invoiceId);
        result.Value.InvoiceNumber.Should().Be("INV-2026-000099");
        result.Value.BillingFailed.Should().BeFalse();
        result.Value.TotalAmount.Should().Be((4m * 20m) + (3m * 5m));

        await _transactionRepository.Received(2).AddAsync(Arg.Any<PharmacyStockTransaction>(), Arg.Any<CancellationToken>());
        // One save for both lines' balance decrements + ledger rows together, one more for
        // linking both transaction rows to the one invoice.
        await _balanceRepository.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _invoiceService.Received(1).CreateAsync(
            Arg.Is<CreateInvoiceRequest>(r => r.Items.Count == 2 && r.Items[0].UnitPrice == 4m * 20m && r.Items[1].UnitPrice == 3m * 5m),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCartAsync_WhenPatientDoesNotExist_ReturnsInvalidPatientFailureWithoutTouchingAnyLine()
    {
        _patientService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<PatientResponse>.Failure("PATIENTS.NOT_FOUND", "not found"));

        var result = await _sut.CreateCartAsync(NewCartRequest(NewLine(_productId, _productBatchId, 4m)), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PharmacyErrorCodes.InvalidPatient);
        await _transactionRepository.DidNotReceive().AddAsync(Arg.Any<PharmacyStockTransaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCartAsync_WhenAMiddleLineHasAnExpiredBatch_FailsTheWholeCartWithoutDispensingAnyLine()
    {
        var secondProductId = Guid.NewGuid();
        var secondBatchId = Guid.NewGuid();
        _productService.GetByIdAsync(secondProductId, Arg.Any<CancellationToken>())
            .Returns(Result<ProductResponse>.Success(new ProductResponse { Id = secondProductId, ProductName = "Amoxicillin 250mg", SellingPrice = 5m }));
        _productBatchService.GetByIdAsync(secondProductId, secondBatchId, Arg.Any<CancellationToken>())
            .Returns(Result<ProductBatchResponse>.Success(new ProductBatchResponse { Id = secondBatchId, ProductId = secondProductId, BatchNo = "B-2025-999", ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1) }));

        var request = NewCartRequest(NewLine(_productId, _productBatchId, 4m), NewLine(secondProductId, secondBatchId, 3m));

        var result = await _sut.CreateCartAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PharmacyErrorCodes.BatchExpired);
        result.Error.Should().StartWith("Line 2:");
        // Nothing was ever added to the change tracker — the failing line was caught in the
        // up-front validation pass, before either line's balance was ever fetched or mutated.
        await _transactionRepository.DidNotReceive().AddAsync(Arg.Any<PharmacyStockTransaction>(), Arg.Any<CancellationToken>());
        await _balanceRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCartAsync_WhenASecondLineHasInsufficientStock_FailsTheWholeCartWithoutDispensingEitherLine()
    {
        var secondProductId = Guid.NewGuid();
        var secondBatchId = Guid.NewGuid();
        _productService.GetByIdAsync(secondProductId, Arg.Any<CancellationToken>())
            .Returns(Result<ProductResponse>.Success(new ProductResponse { Id = secondProductId, ProductName = "Amoxicillin 250mg", SellingPrice = 5m }));
        _productBatchService.GetByIdAsync(secondProductId, secondBatchId, Arg.Any<CancellationToken>())
            .Returns(Result<ProductBatchResponse>.Success(new ProductBatchResponse { Id = secondBatchId, ProductId = secondProductId, BatchNo = "B-2026-777", ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1) }));
        var secondBalance = PharmacyStockBalance.Create(secondProductId, secondBatchId, null);
        secondBalance.Receive(2m, null);
        _balanceRepository.GetByProductAndBatchAsync(secondProductId, secondBatchId, Arg.Any<CancellationToken>()).Returns(secondBalance);

        // First line has plenty of stock (10 on hand, wants 4) — the check must still fail the
        // whole cart because the SECOND line only has 2 on hand but wants 3.
        var request = NewCartRequest(NewLine(_productId, _productBatchId, 4m), NewLine(secondProductId, secondBatchId, 3m));

        var result = await _sut.CreateCartAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PharmacyErrorCodes.InsufficientStock);
        result.Error.Should().StartWith("Line 2:");
        // Line 1 passed its stock check in pass 1, but pass 2 (the actual mutation) never runs
        // because pass 1 must fully clear for every line first — line 1's balance is untouched.
        await _transactionRepository.DidNotReceive().AddAsync(Arg.Any<PharmacyStockTransaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCartAsync_WithDuplicateProductAndBatch_IsRejectedByTheValidatorNotTheService()
    {
        // The duplicate-line rule lives in CreateDispenseCartRequestValidator (see
        // CreateDispenseCartRequestValidatorTests) — DispenseService itself doesn't re-check
        // for duplicates, so calling it directly with a duplicate pair would just dispense both
        // lines against the same balance sequentially. This test documents that boundary.
        var request = NewCartRequest(NewLine(_productId, _productBatchId, 2m), NewLine(_productId, _productBatchId, 2m));

        var result = await _sut.CreateCartAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Lines.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateCartAsync_WhenBillingFails_StillDispensesAllLinesButReportsTheFailureOnEveryLine()
    {
        _invoiceService.CreateAsync(Arg.Any<CreateInvoiceRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Result<InvoiceResponse>.Failure("BILLING.SOME_ERROR", "Billing is temporarily unavailable."));

        var request = NewCartRequest(NewLine(_productId, _productBatchId, 4m));

        var result = await _sut.CreateCartAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Lines[0].BalanceAfter.Should().Be(6m);
        result.Value.BillingFailed.Should().BeTrue();
        result.Value.BillingError.Should().Be("Billing is temporarily unavailable.");
        result.Value.InvoiceId.Should().BeNull();
        result.Value.Lines[0].BillingFailed.Should().BeTrue();

        // Only the one save for the dispense itself — no second save attempting to persist an
        // InvoiceId that was never obtained.
        await _balanceRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCartAsync_WhenFirstSaveThrowsConcurrencyException_RetriesAllLinesAgainstRefreshedBalancesAndSucceeds()
    {
        var secondProductId = Guid.NewGuid();
        var secondBatchId = Guid.NewGuid();
        _productService.GetByIdAsync(secondProductId, Arg.Any<CancellationToken>())
            .Returns(Result<ProductResponse>.Success(new ProductResponse { Id = secondProductId, ProductName = "Amoxicillin 250mg", SellingPrice = 5m }));
        _productBatchService.GetByIdAsync(secondProductId, secondBatchId, Arg.Any<CancellationToken>())
            .Returns(Result<ProductBatchResponse>.Success(new ProductBatchResponse { Id = secondBatchId, ProductId = secondProductId, BatchNo = "B-2026-777", ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1) }));

        var staleFirst = PharmacyStockBalance.Create(_productId, _productBatchId, null);
        staleFirst.Receive(10m, null);
        var refreshedFirst = PharmacyStockBalance.Create(_productId, _productBatchId, null);
        refreshedFirst.Receive(10m, null);
        _balanceRepository.GetByProductAndBatchAsync(_productId, _productBatchId, Arg.Any<CancellationToken>())
            .Returns(staleFirst, refreshedFirst);

        var staleSecond = PharmacyStockBalance.Create(secondProductId, secondBatchId, null);
        staleSecond.Receive(20m, null);
        var refreshedSecond = PharmacyStockBalance.Create(secondProductId, secondBatchId, null);
        refreshedSecond.Receive(20m, null);
        _balanceRepository.GetByProductAndBatchAsync(secondProductId, secondBatchId, Arg.Any<CancellationToken>())
            .Returns(staleSecond, refreshedSecond);

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

        var request = NewCartRequest(NewLine(_productId, _productBatchId, 4m), NewLine(secondProductId, secondBatchId, 3m));

        var result = await _sut.CreateCartAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Lines[0].BalanceAfter.Should().Be(6m);
        result.Value.Lines[1].BalanceAfter.Should().Be(17m);

        // Both balances re-fetched (and thus reloaded) on both attempts.
        await _balanceRepository.Received(2).GetByProductAndBatchAsync(_productId, _productBatchId, Arg.Any<CancellationToken>());
        await _balanceRepository.Received(2).GetByProductAndBatchAsync(secondProductId, secondBatchId, Arg.Any<CancellationToken>());
        // Attempt 1 adds 2 candidate rows (both detached on conflict), attempt 2 adds 2 more
        // that persist = 4 total AddAsync calls, 2 Detach calls.
        await _transactionRepository.Received(4).AddAsync(Arg.Any<PharmacyStockTransaction>(), Arg.Any<CancellationToken>());
        _transactionRepository.Received(2).Detach(Arg.Any<PharmacyStockTransaction>());
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

    [Fact]
    public async Task CreateCartAsync_WhenRetryFindsInsufficientRefreshedStock_ReturnsInsufficientStockAndDetachesAllAbandonedTransactions()
    {
        var secondProductId = Guid.NewGuid();
        var secondBatchId = Guid.NewGuid();
        _productService.GetByIdAsync(secondProductId, Arg.Any<CancellationToken>())
            .Returns(Result<ProductResponse>.Success(new ProductResponse { Id = secondProductId, ProductName = "Amoxicillin 250mg", SellingPrice = 5m }));
        _productBatchService.GetByIdAsync(secondProductId, secondBatchId, Arg.Any<CancellationToken>())
            .Returns(Result<ProductBatchResponse>.Success(new ProductBatchResponse { Id = secondBatchId, ProductId = secondProductId, BatchNo = "B-2026-777", ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1) }));

        var staleFirst = PharmacyStockBalance.Create(_productId, _productBatchId, null);
        staleFirst.Receive(10m, null);
        // The winner of the race dispensed enough of the FIRST product/batch that, once
        // re-fetched on the retry, this cart's request for it no longer fits.
        var refreshedFirst = PharmacyStockBalance.Create(_productId, _productBatchId, null);
        refreshedFirst.Receive(3m, null);
        _balanceRepository.GetByProductAndBatchAsync(_productId, _productBatchId, Arg.Any<CancellationToken>())
            .Returns(staleFirst, refreshedFirst);

        var staleSecond = PharmacyStockBalance.Create(secondProductId, secondBatchId, null);
        staleSecond.Receive(20m, null);
        var refreshedSecond = PharmacyStockBalance.Create(secondProductId, secondBatchId, null);
        refreshedSecond.Receive(20m, null);
        _balanceRepository.GetByProductAndBatchAsync(secondProductId, secondBatchId, Arg.Any<CancellationToken>())
            .Returns(staleSecond, refreshedSecond);

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

        var request = NewCartRequest(NewLine(_productId, _productBatchId, 5m), NewLine(secondProductId, secondBatchId, 3m));

        var result = await _sut.CreateCartAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PharmacyErrorCodes.InsufficientStock);
        result.Error.Should().StartWith("Line 1:");
        // Attempt 1 added candidate rows for both lines before its SaveChangesAsync hit the
        // concurrency conflict; both must be detached so nothing lingers half-tracked once
        // attempt 2's pass 1 fails the (now-refreshed) first line's quantity check and returns
        // before pass 2 ever adds a replacement for either line.
        await _transactionRepository.Received(2).AddAsync(Arg.Any<PharmacyStockTransaction>(), Arg.Any<CancellationToken>());
        _transactionRepository.Received(2).Detach(Arg.Any<PharmacyStockTransaction>());
    }
}
