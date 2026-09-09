using FluentAssertions;
using HMS.Modules.DischargeSummary.Contracts;
using Xunit;
using DischargeMedicationEntity = HMS.Modules.DischargeSummary.Domain.DischargeMedication;

namespace HMS.UnitTests.Modules.DischargeSummary.Domain;

public class DischargeMedicationTests
{
    [Fact]
    public void Create_WithValidArguments_ReturnsMedicationWithTrimmedFields()
    {
        var dischargeSummaryId = Guid.NewGuid();

        var medication = DischargeMedicationEntity.Create(
            dischargeSummaryId,
            sortOrder: 1,
            drugName: "  Paracetamol  ",
            dose: " 500mg ",
            route: " Oral ",
            morningQty: 1,
            noonQty: 0,
            eveningQty: 1,
            nightQty: 1,
            durationDays: 5,
            foodInstruction: FoodInstruction.AfterFood,
            createdBy: null);

        medication.DischargeSummaryId.Should().Be(dischargeSummaryId);
        medication.SortOrder.Should().Be(1);
        medication.DrugName.Should().Be("Paracetamol");
        medication.Dose.Should().Be("500mg");
        medication.Route.Should().Be("Oral");
        medication.MorningQty.Should().Be(1);
        medication.NoonQty.Should().Be(0);
        medication.EveningQty.Should().Be(1);
        medication.NightQty.Should().Be(1);
        medication.DurationDays.Should().Be(5);
        medication.FoodInstruction.Should().Be(FoodInstruction.AfterFood);
        medication.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("", "500mg", "Oral")]
    [InlineData("Paracetamol", "", "Oral")]
    [InlineData("Paracetamol", "500mg", "")]
    public void Create_WithMissingRequiredField_Throws(string drugName, string dose, string route)
    {
        var act = () => DischargeMedicationEntity.Create(
            Guid.NewGuid(), 1, drugName, dose, route, 1, 1, 1, 1, 5, FoodInstruction.BeforeFood, null);

        act.Should().Throw<ArgumentException>();
    }
}
