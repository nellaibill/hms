using FluentAssertions;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.Patients.Domain;

public class PatientVisitTests
{
    private static readonly Guid PatientId = Guid.NewGuid();

    [Fact]
    public void Create_SetsFieldsAndCreatedAudit()
    {
        var actorId = Guid.NewGuid();
        var appointmentTypeId = Guid.NewGuid();

        var visit = PatientVisit.Create(PatientId, VisitType.OP, appointmentTypeId, actorId);

        visit.PatientId.Should().Be(PatientId);
        visit.VisitType.Should().Be(VisitType.OP);
        visit.AppointmentTypeId.Should().Be(appointmentTypeId);
        visit.Consultations.Should().BeEmpty();
        visit.IsDeleted.Should().BeFalse();
        visit.CreatedBy.Should().Be(actorId);
        visit.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_KeepsTheVisitNumberItIsGiven()
    {
        var visit = PatientVisit.Create(PatientId, VisitType.IP, appointmentTypeId: null, createdBy: null, visitNumber: "IP-2026-000001");

        visit.VisitNumber.Should().Be("IP-2026-000001");
    }

    [Fact]
    public void AssignVisitNumber_OnAVisitWithNone_StoresItAndMarksTheVisitUpdated()
    {
        var actorId = Guid.NewGuid();
        var visit = PatientVisit.Create(PatientId, VisitType.OP, appointmentTypeId: null, createdBy: null);

        var assigned = visit.AssignVisitNumber(" OP-2026-000042 ", actorId);

        assigned.Should().BeTrue();
        visit.VisitNumber.Should().Be("OP-2026-000042");
        visit.VisitNumberPending.Should().BeFalse();
        visit.UpdatedBy.Should().Be(actorId);
    }

    // ADR-104: a visit recorded without a number is waiting for one; one recorded with a number
    // (an Emergency visit) is not.
    [Fact]
    public void Create_WithoutANumber_IsPendingAndWithOne_IsNot()
    {
        PatientVisit.Create(PatientId, VisitType.IP, appointmentTypeId: null, createdBy: null).VisitNumberPending.Should().BeTrue();
        PatientVisit.Create(PatientId, VisitType.Emergency, appointmentTypeId: null, createdBy: null, visitNumber: "IP-2026-000001")
            .VisitNumberPending.Should().BeFalse();
    }

    // ADR-103: a second payment for the same visit must never renumber it.
    [Fact]
    public void AssignVisitNumber_OnAVisitThatAlreadyHasOne_KeepsTheFirst()
    {
        var visit = PatientVisit.Create(PatientId, VisitType.OP, appointmentTypeId: null, createdBy: null);
        visit.AssignVisitNumber("OP-2026-000042", updatedBy: null);

        var assigned = visit.AssignVisitNumber("OP-2026-000043", updatedBy: null);

        assigned.Should().BeFalse();
        visit.VisitNumber.Should().Be("OP-2026-000042");
    }

    [Fact]
    public void SetEmergencyType_OnAnEmergencyVisit_StoresIt()
    {
        var visit = PatientVisit.Create(PatientId, VisitType.Emergency, appointmentTypeId: null, createdBy: null);

        visit.SetEmergencyType(EmergencyType.LR);

        visit.EmergencyType.Should().Be(EmergencyType.LR);
    }

    [Theory]
    [InlineData(VisitType.OP)]
    [InlineData(VisitType.IP)]
    [InlineData(VisitType.DayCare)]
    [InlineData(VisitType.Observation)]
    public void SetEmergencyType_OnAnyOtherVisit_StoresNothing(VisitType visitType)
    {
        var visit = PatientVisit.Create(PatientId, visitType, appointmentTypeId: null, createdBy: null);

        visit.SetEmergencyType(EmergencyType.ICU);

        visit.EmergencyType.Should().BeNull();
    }

    [Fact]
    public void SetAdmissionDetails_OnANonOpVisit_StoresEveryAnswer()
    {
        var visit = PatientVisit.Create(PatientId, VisitType.Emergency, appointmentTypeId: null, createdBy: null);

        visit.SetAdmissionDetails(MedicoLegalType.MLC, "  AR/2026/1024  ", isReferred: true, ReferralSource.Ambulance, "  Dr. R. Kumar  ", " 9876543210 ", VisitCategory.B);

        visit.AdmissionType.Should().Be(MedicoLegalType.MLC);
        visit.ArNumber.Should().Be("AR/2026/1024");
        visit.IsReferred.Should().BeTrue();
        visit.ReferredBy.Should().Be(ReferralSource.Ambulance);
        visit.ReferralName.Should().Be("Dr. R. Kumar");
        visit.ReferralMobile.Should().Be("9876543210");
        visit.Category.Should().Be(VisitCategory.B);
    }

    [Fact]
    public void SetAdmissionDetails_DropsAnswersTheirParentAnswerDoesNotCallFor()
    {
        var visit = PatientVisit.Create(PatientId, VisitType.DayCare, appointmentTypeId: null, createdBy: null);

        // An A.R number belongs to an MLC, a referrer to a referred patient.
        visit.SetAdmissionDetails(MedicoLegalType.NMLC, "AR/2026/1024", isReferred: false, ReferralSource.Doctor, "Dr. R. Kumar", "9876543210", category: null);

        visit.AdmissionType.Should().Be(MedicoLegalType.NMLC);
        visit.ArNumber.Should().BeNull();
        visit.IsReferred.Should().BeFalse();
        visit.ReferredBy.Should().BeNull();
        visit.ReferralName.Should().BeNull();
        visit.ReferralMobile.Should().BeNull();
    }

    [Fact]
    public void SetAdmissionDetails_OnAnOpVisit_StoresNothing()
    {
        var visit = PatientVisit.Create(PatientId, VisitType.OP, appointmentTypeId: null, createdBy: null);

        visit.SetAdmissionDetails(MedicoLegalType.MLC, "AR/2026/1024", isReferred: true, ReferralSource.Doctor, "Dr. R. Kumar", "9876543210", VisitCategory.A);

        visit.AdmissionType.Should().BeNull();
        visit.ArNumber.Should().BeNull();
        visit.IsReferred.Should().BeNull();
        visit.ReferredBy.Should().BeNull();
        visit.Category.Should().BeNull();
    }

    [Fact]
    public void AddConsultation_MultipleLines_AllShareTheVisitId()
    {
        var visit = PatientVisit.Create(PatientId, VisitType.OP, appointmentTypeId: null, createdBy: null);

        visit.AddConsultation(PatientVisitConsultation.Create(visit.Id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), updatedBy: null);
        visit.AddConsultation(PatientVisitConsultation.Create(visit.Id, Guid.NewGuid(), Guid.NewGuid(), consultationTypeId: null), updatedBy: null);

        visit.Consultations.Should().HaveCount(2);
        visit.Consultations.Should().OnlyContain(c => c.VisitId == visit.Id);
    }
}
