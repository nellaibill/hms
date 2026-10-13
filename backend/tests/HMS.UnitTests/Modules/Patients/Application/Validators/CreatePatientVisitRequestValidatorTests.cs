using FluentAssertions;
using HMS.Modules.Patients.Application.Validators;
using HMS.Modules.Patients.Contracts;
using Xunit;

namespace HMS.UnitTests.Modules.Patients.Application.Validators;

// Admission details on a non-OP visit: an answer is never accepted half-given (MLC without its
// A.R number, "referred" without a referrer), but the details as a whole stay optional — the
// New Patient Registration wizard records non-OP visits without collecting them yet.
public class CreatePatientVisitRequestValidatorTests
{
    private static CreatePatientVisitRequest NewRequest(VisitType visitType) => new()
    {
        VisitType = visitType,
        Consultations = [new VisitConsultationRequest { DepartmentId = Guid.NewGuid(), ConsultantId = Guid.NewGuid() }],
    };

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Mlc_WithoutArNumber_Fails(string? arNumber)
    {
        var request = NewRequest(VisitType.IP) with { AdmissionType = MedicoLegalType.MLC, ArNumber = arNumber };

        var result = new CreatePatientVisitRequestValidator().Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ArNumber");
    }

    [Fact]
    public void Mlc_WithArNumber_Passes()
    {
        var request = NewRequest(VisitType.Emergency) with { AdmissionType = MedicoLegalType.MLC, ArNumber = "AR/2026/1024" };

        new CreatePatientVisitRequestValidator().Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Nmlc_WithoutArNumber_Passes()
    {
        var request = NewRequest(VisitType.DayCare) with { AdmissionType = MedicoLegalType.NMLC };

        new CreatePatientVisitRequestValidator().Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Referred_WithoutReferrer_Fails()
    {
        var request = NewRequest(VisitType.Observation) with { IsReferred = true };

        var result = new CreatePatientVisitRequestValidator().Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReferredBy");
    }

    [Fact]
    public void Referred_WithReferrer_Passes()
    {
        var request = NewRequest(VisitType.IP) with { IsReferred = true, ReferredBy = ReferralSource.Doctor };

        new CreatePatientVisitRequestValidator().Validate(request).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(VisitType.IP)]
    [InlineData(VisitType.Emergency)]
    [InlineData(VisitType.DayCare)]
    [InlineData(VisitType.Observation)]
    public void NonOpVisit_WithNoAdmissionDetailsAtAll_Passes(VisitType visitType)
    {
        new CreatePatientVisitRequestValidator().Validate(NewRequest(visitType)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void OpVisit_IgnoresAdmissionDetailRules()
    {
        var request = NewRequest(VisitType.OP) with { AdmissionType = MedicoLegalType.MLC, IsReferred = true };

        new CreatePatientVisitRequestValidator().Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ArNumber_LongerThanFiftyCharacters_Fails()
    {
        var request = NewRequest(VisitType.IP) with { AdmissionType = MedicoLegalType.MLC, ArNumber = new string('9', 51) };

        var result = new CreatePatientVisitRequestValidator().Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ArNumber");
    }
}
