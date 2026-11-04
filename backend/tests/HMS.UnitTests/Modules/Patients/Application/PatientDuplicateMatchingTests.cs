using FluentAssertions;
using HMS.Modules.Patients.Application;
using Xunit;

namespace HMS.UnitTests.Modules.Patients.Application;

public class PatientDuplicateMatchingTests
{
    [Theory]
    [InlineData("Jeya Sathya", "", "jeya sathya")]
    [InlineData("Jeya", "Sathya", "jeya sathya")]
    [InlineData("  JEYA ", " Sathya  ", "jeya sathya")]
    [InlineData("Jeya  Sathya", null, "jeya sathya")]
    public void FullNameKey_IgnoresCaseSpacingAndTheFirstLastSplit(string firstName, string? lastName, string expected)
    {
        PatientDuplicateMatching.FullNameKey(firstName, lastName).Should().Be(expected);
    }

    [Fact]
    public void IsSamePatient_ImportedRecordWithWholeNameInFirstNameAndNoId_MatchesARegistrationWithAnId()
    {
        // The legacy shape: full name in FirstName, blank LastName, no ID number — registration
        // now always sends an ID number and a separate last name.
        PatientDuplicateMatching.IsSamePatient("Jeya Sathya", "", null, "Jeya", "Sathya", "123456789012")
            .Should().BeTrue();
    }

    [Fact]
    public void IsSamePatient_SameNameAndSameId_Matches()
    {
        PatientDuplicateMatching.IsSamePatient("Kala", "Devi", "123456789012", "kala", "DEVI", " 123456789012 ")
            .Should().BeTrue();
    }

    [Fact]
    public void IsSamePatient_SameNameButDifferentIdNumbers_IsADifferentPerson()
    {
        // e.g. a father and son with the same name sharing one phone.
        PatientDuplicateMatching.IsSamePatient("Ravi", "Kumar", "111111111111", "Ravi", "Kumar", "222222222222")
            .Should().BeFalse();
    }

    [Fact]
    public void IsSamePatient_NoIdEnteredAndExistingHasOne_MatchesOnNameAlone()
    {
        PatientDuplicateMatching.IsSamePatient("Ravi", "Kumar", "111111111111", "Ravi", "Kumar", null)
            .Should().BeTrue();
    }

    [Theory]
    [InlineData("Kala", "", "Kala", "Devi")]
    [InlineData("Kala Devi", "", "Kamala", "Devi")]
    [InlineData("Ravi", "Kumar", "Ravi", "Kumar S")]
    public void IsSamePatient_DifferentFullName_DoesNotMatch(string existingFirst, string existingLast, string firstName, string lastName)
    {
        PatientDuplicateMatching.IsSamePatient(existingFirst, existingLast, null, firstName, lastName, "123456789012")
            .Should().BeFalse();
    }
}
