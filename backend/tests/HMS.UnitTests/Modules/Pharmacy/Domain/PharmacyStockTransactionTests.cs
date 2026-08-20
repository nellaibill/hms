using FluentAssertions;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.Pharmacy.Domain;

public class PharmacyStockTransactionTests
{
    [Fact]
    public void CreateReceipt_SetsFieldsWithNullPatientAndAdmission()
    {
        var productId = Guid.NewGuid();
        var productBatchId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var transaction = PharmacyStockTransaction.CreateReceipt(productId, productBatchId, 10m, 10m, "Initial stock", actorId);

        transaction.ProductId.Should().Be(productId);
        transaction.ProductBatchId.Should().Be(productBatchId);
        transaction.TransactionType.Should().Be(TransactionType.Receipt);
        transaction.Quantity.Should().Be(10m);
        transaction.BalanceAfter.Should().Be(10m);
        transaction.PatientId.Should().BeNull();
        transaction.AdmissionId.Should().BeNull();
        transaction.Remarks.Should().Be("Initial stock");
        transaction.CreatedBy.Should().Be(actorId);
        transaction.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void CreateDispense_SetsPatientAndOptionalAdmission()
    {
        var productId = Guid.NewGuid();
        var productBatchId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var admissionId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var transaction = PharmacyStockTransaction.CreateDispense(productId, productBatchId, 4m, 6m, patientId, admissionId, "OPD dispense", actorId);

        transaction.TransactionType.Should().Be(TransactionType.Dispense);
        transaction.Quantity.Should().Be(4m);
        transaction.BalanceAfter.Should().Be(6m);
        transaction.PatientId.Should().Be(patientId);
        transaction.AdmissionId.Should().Be(admissionId);
        transaction.Remarks.Should().Be("OPD dispense");
    }

    [Fact]
    public void CreateDispense_WithoutAdmission_LeavesAdmissionIdNull()
    {
        var transaction = PharmacyStockTransaction.CreateDispense(Guid.NewGuid(), Guid.NewGuid(), 1m, 0m, Guid.NewGuid(), admissionId: null, remarks: null, createdBy: null);

        transaction.AdmissionId.Should().BeNull();
        transaction.Remarks.Should().BeNull();
    }

    [Fact]
    public void CreateReceipt_WithWhitespaceRemarks_StoresNull()
    {
        var transaction = PharmacyStockTransaction.CreateReceipt(Guid.NewGuid(), Guid.NewGuid(), 1m, 1m, "   ", null);

        transaction.Remarks.Should().BeNull();
    }

    [Fact]
    public void CreateReceipt_TrimsRemarks()
    {
        var transaction = PharmacyStockTransaction.CreateReceipt(Guid.NewGuid(), Guid.NewGuid(), 1m, 1m, "  restock  ", null);

        transaction.Remarks.Should().Be("restock");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateReceipt_WithNonPositiveQuantity_ThrowsArgumentOutOfRangeException(decimal quantity)
    {
        var act = () => PharmacyStockTransaction.CreateReceipt(Guid.NewGuid(), Guid.NewGuid(), quantity, 0m, null, null);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateDispense_WithNonPositiveQuantity_ThrowsArgumentOutOfRangeException(decimal quantity)
    {
        var act = () => PharmacyStockTransaction.CreateDispense(Guid.NewGuid(), Guid.NewGuid(), quantity, 0m, Guid.NewGuid(), null, null, null);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SetInvoiceId_OnADispense_SetsIt()
    {
        var transaction = PharmacyStockTransaction.CreateDispense(Guid.NewGuid(), Guid.NewGuid(), 4m, 6m, Guid.NewGuid(), null, null, null);
        var invoiceId = Guid.NewGuid();

        transaction.SetInvoiceId(invoiceId, null);

        transaction.InvoiceId.Should().Be(invoiceId);
    }

    [Fact]
    public void SetInvoiceId_OnAReceipt_Throws()
    {
        var transaction = PharmacyStockTransaction.CreateReceipt(Guid.NewGuid(), Guid.NewGuid(), 10m, 10m, null, null);

        var act = () => transaction.SetInvoiceId(Guid.NewGuid(), null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SetInvoiceId_CalledTwice_ThrowsOnTheSecondCall()
    {
        var transaction = PharmacyStockTransaction.CreateDispense(Guid.NewGuid(), Guid.NewGuid(), 4m, 6m, Guid.NewGuid(), null, null, null);
        transaction.SetInvoiceId(Guid.NewGuid(), null);

        var act = () => transaction.SetInvoiceId(Guid.NewGuid(), null);

        act.Should().Throw<InvalidOperationException>();
    }
}
