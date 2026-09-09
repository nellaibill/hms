using FluentAssertions;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.IPD.Domain;

public class DoctorOrderTests
{
    private readonly Guid _admissionId = Guid.NewGuid();

    [Fact]
    public void Create_StartsInOrderedStatus()
    {
        var order = DoctorOrder.Create(_admissionId, DoctorOrderType.Radiology, "Chest X-ray", null, DateTime.UtcNow, null, null);

        order.Status.Should().Be(DoctorOrderStatus.Ordered);
        order.Description.Should().Be("Chest X-ray");
    }

    [Fact]
    public void Create_WithBlankDescription_Throws()
    {
        var act = () => DoctorOrder.Create(_admissionId, DoctorOrderType.Diet, "  ", null, DateTime.UtcNow, null, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Advance_MovesThroughFullSequence()
    {
        var order = DoctorOrder.Create(_admissionId, DoctorOrderType.Procedure, "Wound dressing", null, DateTime.UtcNow, null, null);

        order.Advance(null);
        order.Status.Should().Be(DoctorOrderStatus.Accepted);

        order.Advance(null);
        order.Status.Should().Be(DoctorOrderStatus.InProgress);

        order.Advance(null);
        order.Status.Should().Be(DoctorOrderStatus.Completed);
        order.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Advance_WhenAlreadyCompleted_Throws()
    {
        var order = DoctorOrder.Create(_admissionId, DoctorOrderType.Consultation, "Cardiology review", null, DateTime.UtcNow, null, null);
        order.Advance(null);
        order.Advance(null);
        order.Advance(null);

        var act = () => order.Advance(null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Advance_WhenCancelled_Throws()
    {
        var order = DoctorOrder.Create(_admissionId, DoctorOrderType.Referral, "ENT referral", null, DateTime.UtcNow, null, null);
        order.Cancel("No longer needed", null);

        var act = () => order.Advance(null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_FromInProgress_Succeeds()
    {
        var order = DoctorOrder.Create(_admissionId, DoctorOrderType.Blood, "2 units PRBC", null, DateTime.UtcNow, null, null);
        order.Advance(null);
        order.Advance(null);

        order.Cancel("Patient stabilized", null);

        order.Status.Should().Be(DoctorOrderStatus.Cancelled);
        order.CancellationReason.Should().Be("Patient stabilized");
        order.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_WhenAlreadyCompleted_Throws()
    {
        var order = DoctorOrder.Create(_admissionId, DoctorOrderType.Nursing, "Repositioning schedule", null, DateTime.UtcNow, null, null);
        order.Advance(null);
        order.Advance(null);
        order.Advance(null);

        var act = () => order.Cancel(null, null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_Throws()
    {
        var order = DoctorOrder.Create(_admissionId, DoctorOrderType.Diet, "Diabetic diet", null, DateTime.UtcNow, null, null);
        order.Cancel(null, null);

        var act = () => order.Cancel(null, null);

        act.Should().Throw<InvalidOperationException>();
    }
}
