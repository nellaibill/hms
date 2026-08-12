using FluentAssertions;
using HMS.Modules.IPD.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.IPD.Domain;

public class BedTransferHistoryTests
{
    [Fact]
    public void Create_SetsFields()
    {
        var admissionId = Guid.NewGuid();
        var oldWardId = Guid.NewGuid();
        var oldBedId = Guid.NewGuid();
        var newWardId = Guid.NewGuid();
        var newBedId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var history = BedTransferHistory.Create(admissionId, oldWardId, oldBedId, newWardId, newBedId, "Ward change per doctor's advice", actorId);

        history.AdmissionId.Should().Be(admissionId);
        history.OldWardId.Should().Be(oldWardId);
        history.OldBedId.Should().Be(oldBedId);
        history.NewWardId.Should().Be(newWardId);
        history.NewBedId.Should().Be(newBedId);
        history.TransferReason.Should().Be("Ward change per doctor's advice");
        history.CreatedBy.Should().Be(actorId);
        history.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithWhitespaceReason_StoresNull()
    {
        var history = BedTransferHistory.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "   ", null);

        history.TransferReason.Should().BeNull();
    }

    [Fact]
    public void Create_TrimsReason()
    {
        var history = BedTransferHistory.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "  ICU escalation  ", null);

        history.TransferReason.Should().Be("ICU escalation");
    }
}
