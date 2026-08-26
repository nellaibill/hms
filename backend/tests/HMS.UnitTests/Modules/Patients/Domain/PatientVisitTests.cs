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
    public void AddConsultation_MultipleLines_AllShareTheVisitId()
    {
        var visit = PatientVisit.Create(PatientId, VisitType.OP, appointmentTypeId: null, createdBy: null);

        visit.AddConsultation(PatientVisitConsultation.Create(visit.Id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), updatedBy: null);
        visit.AddConsultation(PatientVisitConsultation.Create(visit.Id, Guid.NewGuid(), Guid.NewGuid(), consultationTypeId: null), updatedBy: null);

        visit.Consultations.Should().HaveCount(2);
        visit.Consultations.Should().OnlyContain(c => c.VisitId == visit.Id);
    }
}
