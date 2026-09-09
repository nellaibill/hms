using FluentAssertions;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.IPD.Domain;

public class AdmissionAdvanceTests
{
    [Fact]
    public void Create_WithValidAmount_SetsAllFields()
    {
        var admissionId = Guid.NewGuid();

        var advance = AdmissionAdvance.Create(admissionId, 5000m, PaymentMethod.Upi, "UPI-REF-001", "Family advance", null);

        advance.AdmissionId.Should().Be(admissionId);
        advance.Amount.Should().Be(5000m);
        advance.Method.Should().Be(PaymentMethod.Upi);
        advance.ReferenceNumber.Should().Be("UPI-REF-001");
        advance.Remarks.Should().Be("Family advance");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Create_WithNonPositiveAmount_Throws(decimal amount)
    {
        var act = () => AdmissionAdvance.Create(Guid.NewGuid(), amount, PaymentMethod.Cash, null, null, null);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
