using FluentAssertions;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.Patients.Domain;

public class PatientVisitConsultationTests
{
    private static readonly Guid VisitId = Guid.NewGuid();
    private static readonly Guid DepartmentId = Guid.NewGuid();
    private static readonly Guid ConsultantId = Guid.NewGuid();

    private static PatientVisitConsultation NewConsultation(DateTime? appointmentTime = null)
        => PatientVisitConsultation.Create(VisitId, DepartmentId, ConsultantId, consultationTypeId: null, appointmentTime);

    [Fact]
    public void Create_WithoutAppointmentTime_DefaultsToNowAndWaiting()
    {
        var consultation = NewConsultation();

        consultation.Status.Should().Be(OpdConsultationStatus.Waiting);
        consultation.AppointmentTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithExplicitAppointmentTime_UsesIt()
    {
        var appointmentTime = new DateTime(2026, 1, 1, 9, 30, 0, DateTimeKind.Utc);

        var consultation = NewConsultation(appointmentTime);

        consultation.AppointmentTime.Should().Be(appointmentTime);
    }

    [Fact]
    public void CheckIn_FromWaiting_TransitionsToCheckedIn()
    {
        var consultation = NewConsultation();

        consultation.CheckIn();

        consultation.Status.Should().Be(OpdConsultationStatus.CheckedIn);
    }

    [Fact]
    public void CheckIn_WhenNotWaiting_Throws()
    {
        var consultation = NewConsultation();
        consultation.CheckIn();

        var act = () => consultation.CheckIn();

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StartConsultation_FromWaitingOrCheckedIn_TransitionsToInConsultation(bool checkInFirst)
    {
        var consultation = NewConsultation();
        if (checkInFirst)
        {
            consultation.CheckIn();
        }

        consultation.StartConsultation();

        consultation.Status.Should().Be(OpdConsultationStatus.InConsultation);
    }

    [Fact]
    public void StartConsultation_WhenAlreadyCompleted_Throws()
    {
        var consultation = NewConsultation();
        consultation.StartConsultation();
        consultation.Complete();

        var act = () => consultation.StartConsultation();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Complete_FromInConsultation_TransitionsToCompleted()
    {
        var consultation = NewConsultation();
        consultation.StartConsultation();

        consultation.Complete();

        consultation.Status.Should().Be(OpdConsultationStatus.Completed);
    }

    [Fact]
    public void Complete_WhenWaiting_Throws()
    {
        var consultation = NewConsultation();

        var act = () => consultation.Complete();

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(OpdConsultationStatus.Waiting)]
    [InlineData(OpdConsultationStatus.CheckedIn)]
    [InlineData(OpdConsultationStatus.InConsultation)]
    public void Cancel_FromAnyNonTerminalState_TransitionsToCancelled(OpdConsultationStatus fromStatus)
    {
        var consultation = NewConsultation();
        MoveTo(consultation, fromStatus);

        consultation.Cancel();

        consultation.Status.Should().Be(OpdConsultationStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenAlreadyCompleted_Throws()
    {
        var consultation = NewConsultation();
        consultation.StartConsultation();
        consultation.Complete();

        var act = () => consultation.Cancel();

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MarkNoShow_FromWaitingOrCheckedIn_TransitionsToNoShow(bool checkInFirst)
    {
        var consultation = NewConsultation();
        if (checkInFirst)
        {
            consultation.CheckIn();
        }

        consultation.MarkNoShow();

        consultation.Status.Should().Be(OpdConsultationStatus.NoShow);
    }

    [Fact]
    public void MarkNoShow_WhenInConsultation_Throws()
    {
        var consultation = NewConsultation();
        consultation.StartConsultation();

        var act = () => consultation.MarkNoShow();

        act.Should().Throw<InvalidOperationException>();
    }

    private static void MoveTo(PatientVisitConsultation consultation, OpdConsultationStatus status)
    {
        switch (status)
        {
            case OpdConsultationStatus.Waiting:
                break;
            case OpdConsultationStatus.CheckedIn:
                consultation.CheckIn();
                break;
            case OpdConsultationStatus.InConsultation:
                consultation.StartConsultation();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }
    }
}
