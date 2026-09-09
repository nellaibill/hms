using FluentAssertions;
using HMS.Modules.Laboratory.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.Laboratory.Domain;

public class LabOrderTests
{
    [Fact]
    public void CreateForAdmission_SetsAdmissionIdAndLeavesInvoiceAndVisitNull()
    {
        var admissionId = Guid.NewGuid();
        var spec = new LabOrderItemSpec(Guid.NewGuid(), null, "Complete Blood Count", null, null, null, null);

        var order = LabOrder.CreateForAdmission(
            "LAB-2026-000001",
            admissionId,
            Guid.NewGuid(),
            "Aravind Nadar",
            "NH20260001",
            [spec],
            createdBy: null);

        order.AdmissionId.Should().Be(admissionId);
        order.InvoiceId.Should().BeNull();
        order.VisitId.Should().BeNull();
        order.Source.Should().Be("IPD");
        order.Items.Should().ContainSingle();
    }

    [Fact]
    public void CreateForAdmission_WithNoItems_Throws()
    {
        var act = () => LabOrder.CreateForAdmission(
            "LAB-2026-000001",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Aravind Nadar",
            "NH20260001",
            [],
            createdBy: null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_FromInvoice_StillLeavesAdmissionIdNull()
    {
        var spec = new LabOrderItemSpec(Guid.NewGuid(), null, "Complete Blood Count", Guid.NewGuid(), null, null, null);

        var order = LabOrder.Create(
            "LAB-2026-000001",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Aravind Nadar",
            "NH20260001",
            Guid.NewGuid(),
            "OP",
            [spec],
            createdBy: null);

        order.AdmissionId.Should().BeNull();
        order.InvoiceId.Should().NotBeNull();
        order.VisitId.Should().NotBeNull();
    }
}
