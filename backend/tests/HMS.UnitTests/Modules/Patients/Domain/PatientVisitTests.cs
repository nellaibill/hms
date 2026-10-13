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
    public void SetAdmissionDetails_OnANonOpVisit_StoresEveryAnswer()
    {
        var visit = PatientVisit.Create(PatientId, VisitType.Emergency, appointmentTypeId: null, createdBy: null);

        visit.SetAdmissionDetails(MedicoLegalType.MLC, "  AR/2026/1024  ", isReferred: true, ReferralSource.Ambulance, VisitCategory.B);

        visit.AdmissionType.Should().Be(MedicoLegalType.MLC);
        visit.ArNumber.Should().Be("AR/2026/1024");
        visit.IsReferred.Should().BeTrue();
        visit.ReferredBy.Should().Be(ReferralSource.Ambulance);
        visit.Category.Should().Be(VisitCategory.B);
    }

    [Fact]
    public void SetAdmissionDetails_DropsAnswersTheirParentAnswerDoesNotCallFor()
    {
        var visit = PatientVisit.Create(PatientId, VisitType.DayCare, appointmentTypeId: null, createdBy: null);

        // An A.R number belongs to an MLC, a referrer to a referred patient.
        visit.SetAdmissionDetails(MedicoLegalType.NMLC, "AR/2026/1024", isReferred: false, ReferralSource.Doctor, category: null);

        visit.AdmissionType.Should().Be(MedicoLegalType.NMLC);
        visit.ArNumber.Should().BeNull();
        visit.IsReferred.Should().BeFalse();
        visit.ReferredBy.Should().BeNull();
    }

    [Fact]
    public void SetAdmissionDetails_OnAnOpVisit_StoresNothing()
    {
        var visit = PatientVisit.Create(PatientId, VisitType.OP, appointmentTypeId: null, createdBy: null);

        visit.SetAdmissionDetails(MedicoLegalType.MLC, "AR/2026/1024", isReferred: true, ReferralSource.Doctor, VisitCategory.A);

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
