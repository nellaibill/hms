using FluentAssertions;
using HMS.Modules.ActivityLog.Application;
using HMS.Modules.Appointments.Application;
using HMS.Modules.Appointments.Application.Abstractions;
using HMS.Modules.Appointments.Contracts;
using HMS.Modules.Appointments.Domain;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Contracts;
using HMS.Shared.Kernel;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Appointments.Application;

public class AppointmentServiceTests
{
    // "Now" is Thursday 15 Oct 2026, 10:00 IST (04:30 UTC).
    private static readonly DateOnly Today = new(2026, 10, 15);
    private static readonly DateTimeOffset NowUtc = new(2026, 10, 15, 4, 30, 0, TimeSpan.Zero);

    private static readonly Guid ConsultantId = Guid.NewGuid();
    private static readonly Guid DepartmentId = Guid.NewGuid();
    private static readonly Guid PatientId = Guid.NewGuid();
    private static readonly Guid VisitId = Guid.NewGuid();

    private readonly IAppointmentRepository _appointments = Substitute.For<IAppointmentRepository>();
    private readonly IConsultantBlockRepository _blocks = Substitute.For<IConsultantBlockRepository>();
    private readonly IAppointmentNumberGenerator _numbers = Substitute.For<IAppointmentNumberGenerator>();
    private readonly IConsultantService _consultants = Substitute.For<IConsultantService>();
    private readonly IDepartmentService _departments = Substitute.For<IDepartmentService>();
    private readonly IAppointmentTypeService _appointmentTypes = Substitute.For<IAppointmentTypeService>();
    private readonly IPatientService _patients = Substitute.For<IPatientService>();
    private readonly IPatientVisitService _visits = Substitute.For<IPatientVisitService>();
    private readonly IActivityLogService _activityLog = Substitute.For<IActivityLogService>();
    private readonly AppointmentService _sut;

    public AppointmentServiceTests()
    {
        _sut = new AppointmentService(
            _appointments, _blocks, _numbers, _consultants, _departments, _appointmentTypes, _patients, _visits, _activityLog,
            new AppointmentClock(new FixedTimeProvider(NowUtc)));

        // Happy-path defaults; each failure test overrides one of these.
        UseConsultant(Consultant());
        _departments.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Result<DepartmentResponse>.Success(new DepartmentResponse()));
        _appointmentTypes.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Result<AppointmentTypeResponse>.Success(new AppointmentTypeResponse()));
        _patients.GetByIdAsync(PatientId, Arg.Any<CancellationToken>()).Returns(Result<PatientResponse>.Success(new PatientResponse
        {
            Id = PatientId,
            Uhid = "P-2026-000123",
            FirstName = "Ravi",
            LastName = "Kumar",
            PrimaryPhone = "9876543210",
        }));
        _visits.GetByIdAsync(PatientId, VisitId, Arg.Any<CancellationToken>())
            .Returns(Result<PatientVisitResponse>.Success(new PatientVisitResponse { VisitId = VisitId, PatientId = PatientId }));

        _numbers.NextAsync(Arg.Any<CancellationToken>()).Returns("APT-2026-000001");
        _appointments.GetTakenSeatsAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<TimeOnly>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<int>());
        _appointments.HasBookingForSameDayAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<Guid?>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _appointments.GetSeatHoldersForDayAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<Appointment>());
        _appointments.GetBookedInRangeAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<Appointment>());
        _appointments.TrySaveChangesAsync(Arg.Any<CancellationToken>()).Returns(true);
        _blocks.GetOverlappingAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<ConsultantBlock>());
    }

    // Clinic Mon–Sat, 09:00–13:00 and 17:00–19:00, 15-minute slots.
    private static ConsultantResponse Consultant(int? slotMinutes = null, int? patientsPerSlot = null, bool isActive = true) => new()
    {
        Id = ConsultantId,
        Name = "Dr. Anand",
        DepartmentId = DepartmentId,
        IsActive = isActive,
        AvailableDays = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"],
        VisitStartTime = new TimeOnly(9, 0),
        VisitEndTime = new TimeOnly(13, 0),
        VisitStartTime2 = new TimeOnly(17, 0),
        VisitEndTime2 = new TimeOnly(19, 0),
        AppointmentSlotMinutes = slotMinutes,
        PatientsPerSlot = patientsPerSlot,
    };

    private void UseConsultant(ConsultantResponse consultant)
        => _consultants.GetByIdAsync(ConsultantId, Arg.Any<CancellationToken>()).Returns(Result<ConsultantResponse>.Success(consultant));

    private static BookAppointmentRequest Request(DateOnly? date = null, TimeOnly? start = null, Guid? patientId = null) => new()
    {
        PatientId = patientId,
        PatientName = patientId is null ? "Meena Raj" : null,
        PhoneNumber = patientId is null ? "9123456780" : null,
        DepartmentId = DepartmentId,
        ConsultantId = ConsultantId,
        AppointmentDate = date ?? Today.AddDays(1),
        StartTime = start ?? new TimeOnly(9, 30),
        Source = AppointmentSource.Phone,
    };

    private static Appointment Booked(DateOnly? date = null, Guid? patientId = null) => Appointment.Book(
        "APT-2026-000009",
        new AppointmentPatient(patientId, "Meena Raj", "9123456780", null),
        DepartmentId,
        ConsultantId,
        null,
        new SlotAssignment(date ?? Today, new TimeOnly(11, 0), new TimeOnly(11, 15), 1, 1, 9),
        AppointmentSource.Phone,
        null,
        null);

    [Fact]
    public async Task Book_ARegisteredPatient_CopiesTheirDetails_AndNumbersTheToken()
    {
        var result = await _sut.BookAsync(Request(patientId: PatientId), null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.PatientName.Should().Be("Ravi Kumar");
        result.Value.PhoneNumber.Should().Be("9876543210");
        result.Value.Uhid.Should().Be("P-2026-000123");
        result.Value.IsNewPatient.Should().BeFalse();
        result.Value.EndTime.Should().Be(new TimeOnly(9, 45));
        result.Value.Session.Should().Be(1);
        result.Value.TokenNumber.Should().Be(3); // 09:00, 09:15, 09:30 → third slot
        await _appointments.Received(1).AddAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
        await _activityLog.ReceivedWithAnyArgs(1).LogAsync(default!, default);
    }

    [Fact]
    public async Task Book_AFirstTimeCaller_KeepsNameAndPhone()
    {
        var result = await _sut.BookAsync(Request(), null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.PatientId.Should().BeNull();
        result.Value.IsNewPatient.Should().BeTrue();
        result.Value.PatientName.Should().Be("Meena Raj");
        await _patients.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
    }

    [Fact]
    public async Task Book_InTheSecondSession_ContinuesTheTokenCount()
    {
        // Session 1 has 16 slots (09:00–13:00), so 17:00 is the 17th.
        var result = await _sut.BookAsync(Request(start: new TimeOnly(17, 0)), null, CancellationToken.None);

        result.Value!.Session.Should().Be(2);
        result.Value.TokenNumber.Should().Be(17);
    }

    [Fact]
    public async Task Book_TakesTheNextFreeSeat_WhenSeveralPatientsShareASlot()
    {
        UseConsultant(Consultant(patientsPerSlot: 2));
        _appointments.GetTakenSeatsAsync(ConsultantId, Arg.Any<DateOnly>(), new TimeOnly(9, 30), null, Arg.Any<CancellationToken>()).Returns(new[] { 1 });

        var result = await _sut.BookAsync(Request(), null, CancellationToken.None);

        result.Value!.TokenNumber.Should().Be(6); // slot index 2 × 2 seats + seat 2
    }

    [Theory]
    [InlineData(-1, 9, 30, "APPOINTMENTS.DATE_IN_PAST")]
    [InlineData(91, 9, 30, "APPOINTMENTS.TOO_FAR_AHEAD")]
    [InlineData(3, 9, 30, "APPOINTMENTS.NOT_A_CLINIC_DAY")] // Sunday
    [InlineData(1, 9, 20, "APPOINTMENTS.INVALID_SLOT")]
    [InlineData(1, 13, 0, "APPOINTMENTS.INVALID_SLOT")]
    [InlineData(0, 9, 30, "APPOINTMENTS.SLOT_IN_PAST")] // 09:30–09:45 ended before 10:00
    public async Task Book_OutsideTheConsultantsBookableSlots_Fails(int daysFromToday, int hour, int minute, string errorCode)
    {
        var result = await _sut.BookAsync(Request(Today.AddDays(daysFromToday), new TimeOnly(hour, minute)), null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(errorCode);
        await _appointments.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Book_TheSlotUnderWayRightNow_IsAllowed()
    {
        var result = await _sut.BookAsync(Request(Today, new TimeOnly(10, 0)), null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Error);
    }

    [Fact]
    public async Task Book_WhenTheConsultantIsOnLeave_Fails()
    {
        var date = Today.AddDays(1);
        _blocks.GetOverlappingAsync(ConsultantId, date, date, Arg.Any<CancellationToken>())
            .Returns(new[] { ConsultantBlock.Create(ConsultantId, date, date, 1, "Conference", null) });

        var morning = await _sut.BookAsync(Request(date, new TimeOnly(9, 30)), null, CancellationToken.None);
        var evening = await _sut.BookAsync(Request(date, new TimeOnly(17, 0)), null, CancellationToken.None);

        morning.ErrorCode.Should().Be("APPOINTMENTS.CONSULTANT_UNAVAILABLE");
        morning.Error.Should().Contain("Conference");
        evening.IsSuccess.Should().BeTrue("the block only closes session 1");
    }

    [Fact]
    public async Task Book_AFullSlot_Fails()
    {
        _appointments.GetTakenSeatsAsync(ConsultantId, Arg.Any<DateOnly>(), new TimeOnly(9, 30), null, Arg.Any<CancellationToken>()).Returns(new[] { 1 });

        var result = await _sut.BookAsync(Request(), null, CancellationToken.None);

        result.ErrorCode.Should().Be("APPOINTMENTS.SLOT_FULL");
    }

    [Fact]
    public async Task Book_WhenAnotherDeskWinsTheSeatAtTheSameMoment_ReportsTheSlotAsFull()
    {
        _appointments.TrySaveChangesAsync(Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.BookAsync(Request(), null, CancellationToken.None);

        result.ErrorCode.Should().Be("APPOINTMENTS.SLOT_FULL");
        await _activityLog.DidNotReceiveWithAnyArgs().LogAsync(default!, default);
    }

    [Fact]
    public async Task Book_TheSamePatientTwiceWithOneConsultantOnOneDay_Fails()
    {
        _appointments.HasBookingForSameDayAsync(ConsultantId, Arg.Any<DateOnly>(), PatientId, "9876543210", "Ravi Kumar", null, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.BookAsync(Request(patientId: PatientId), null, CancellationToken.None);

        result.ErrorCode.Should().Be("APPOINTMENTS.DUPLICATE_BOOKING");
    }

    [Fact]
    public async Task Book_WithAnInactiveConsultant_Fails()
    {
        UseConsultant(Consultant(isActive: false));

        var result = await _sut.BookAsync(Request(), null, CancellationToken.None);

        result.ErrorCode.Should().Be("APPOINTMENTS.CONSULTANT_INACTIVE");
    }

    [Fact]
    public async Task Book_UsesTheConsultantsOwnSlotLength()
    {
        UseConsultant(Consultant(slotMinutes: 20));

        var ok = await _sut.BookAsync(Request(start: new TimeOnly(9, 40)), null, CancellationToken.None);
        var notASlot = await _sut.BookAsync(Request(start: new TimeOnly(9, 30)), null, CancellationToken.None);

        ok.Value!.EndTime.Should().Be(new TimeOnly(10, 0));
        notASlot.ErrorCode.Should().Be("APPOINTMENTS.INVALID_SLOT");
    }

    [Fact]
    public async Task Reschedule_MovesTheBooking_IgnoringItsOwnSeat()
    {
        var appointment = Booked(Today.AddDays(1));
        _appointments.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);

        var result = await _sut.RescheduleAsync(appointment.Id, new RescheduleAppointmentRequest { AppointmentDate = Today.AddDays(2), StartTime = new TimeOnly(9, 0) }, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.AppointmentDate.Should().Be(Today.AddDays(2));
        result.Value.TokenNumber.Should().Be(1);
        result.Value.RescheduleCount.Should().Be(1);
        await _appointments.Received(1).GetTakenSeatsAsync(ConsultantId, Today.AddDays(2), new TimeOnly(9, 0), appointment.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancel_ACancelledAppointment_IsRejected()
    {
        var appointment = Booked();
        appointment.Cancel("Called", null);
        _appointments.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);

        var result = await _sut.CancelAsync(appointment.Id, new CancelAppointmentRequest { Reason = "Again" }, null, CancellationToken.None);

        result.ErrorCode.Should().Be("APPOINTMENTS.INVALID_STATUS");
    }

    [Fact]
    public async Task MarkNoShow_BeforeTheAppointmentDay_IsRejected()
    {
        var appointment = Booked(Today.AddDays(1));
        _appointments.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);

        var result = await _sut.MarkNoShowAsync(appointment.Id, null, CancellationToken.None);

        result.ErrorCode.Should().Be("APPOINTMENTS.WRONG_DAY");
        appointment.Status.Should().Be(AppointmentStatus.Booked);
    }

    [Fact]
    public async Task MarkArrived_OnTheDay_LinksTheVisitAndTheRegisteredPatient()
    {
        var appointment = Booked(Today);
        _appointments.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);

        var result = await _sut.MarkArrivedAsync(appointment.Id, new MarkAppointmentArrivedRequest { PatientId = PatientId, VisitId = VisitId }, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.Status.Should().Be(AppointmentStatus.Arrived);
        result.Value.PatientId.Should().Be(PatientId);
        result.Value.Uhid.Should().Be("P-2026-000123");
        result.Value.VisitId.Should().Be(VisitId);
    }

    [Fact]
    public async Task MarkArrived_OnAnotherDay_IsRejected()
    {
        var appointment = Booked(Today.AddDays(1));
        _appointments.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);

        var result = await _sut.MarkArrivedAsync(appointment.Id, new MarkAppointmentArrivedRequest { PatientId = PatientId, VisitId = VisitId }, null, CancellationToken.None);

        result.ErrorCode.Should().Be("APPOINTMENTS.WRONG_DAY");
    }

    [Fact]
    public async Task MarkArrived_ForADifferentPatient_IsRejected()
    {
        var appointment = Booked(Today, patientId: Guid.NewGuid());
        _appointments.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);

        var result = await _sut.MarkArrivedAsync(appointment.Id, new MarkAppointmentArrivedRequest { PatientId = PatientId, VisitId = VisitId }, null, CancellationToken.None);

        result.ErrorCode.Should().Be("APPOINTMENTS.PATIENT_MISMATCH");
    }

    [Fact]
    public async Task MarkArrived_WithAVisitThatIsNotThePatients_IsRejected()
    {
        var appointment = Booked(Today);
        _appointments.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);
        var otherVisit = Guid.NewGuid();
        _visits.GetByIdAsync(PatientId, otherVisit, Arg.Any<CancellationToken>()).Returns(Result<PatientVisitResponse>.Failure("PATIENTS.VISIT_NOT_FOUND", "not found"));

        var result = await _sut.MarkArrivedAsync(appointment.Id, new MarkAppointmentArrivedRequest { PatientId = PatientId, VisitId = otherVisit }, null, CancellationToken.None);

        result.ErrorCode.Should().Be("APPOINTMENTS.INVALID_VISIT");
    }

    [Fact]
    public async Task GetSlots_MarksBookedPastAndBlockedSlots()
    {
        var holder = Appointment.Book(
            "APT-2026-000010",
            new AppointmentPatient(null, "Meena Raj", "9123456780", null),
            DepartmentId,
            ConsultantId,
            null,
            new SlotAssignment(Today, new TimeOnly(11, 0), new TimeOnly(11, 15), 1, 1, 9),
            AppointmentSource.Phone,
            null,
            null);
        _appointments.GetSeatHoldersForDayAsync(ConsultantId, Today, Arg.Any<CancellationToken>()).Returns(new[] { holder });
        _blocks.GetOverlappingAsync(ConsultantId, Today, Today, Arg.Any<CancellationToken>())
            .Returns(new[] { ConsultantBlock.Create(ConsultantId, Today, Today, 2, "Evening OT list", null) });

        var result = await _sut.GetSlotsAsync(new AppointmentSlotsQuery { ConsultantId = ConsultantId, Date = Today }, CancellationToken.None);

        var slots = result.Value!.Slots;
        slots.Should().HaveCount(24);
        slots.Single(s => s.StartTime == new TimeOnly(9, 0)).IsPast.Should().BeTrue();
        slots.Single(s => s.StartTime == new TimeOnly(10, 0)).IsBookable.Should().BeTrue();
        var eleven = slots.Single(s => s.StartTime == new TimeOnly(11, 0));
        eleven.Booked.Should().Be(1);
        eleven.IsBookable.Should().BeFalse();
        var evening = slots.Single(s => s.StartTime == new TimeOnly(17, 0));
        evening.IsBlocked.Should().BeTrue();
        evening.BlockReason.Should().Be("Evening OT list");
        result.Value.Message.Should().BeNull("only the evening is blocked");
    }

    [Fact]
    public async Task GetSlots_OnADayWithoutClinic_ExplainsWhy()
    {
        var sunday = Today.AddDays(3);

        var result = await _sut.GetSlotsAsync(new AppointmentSlotsQuery { ConsultantId = ConsultantId, Date = sunday }, CancellationToken.None);

        result.Value!.IsWorkingDay.Should().BeFalse();
        result.Value.Slots.Should().BeEmpty();
        result.Value.Message.Should().Contain("Sundays");
    }

    [Fact]
    public async Task CreateBlock_ListsTheBookingsThatNowNeedMoving()
    {
        var date = Today.AddDays(1);
        var morning = Booked(date);
        _appointments.GetBookedInRangeAsync(ConsultantId, date, date, Arg.Any<CancellationToken>()).Returns(new[] { morning });

        var result = await _sut.CreateBlockAsync(
            new CreateConsultantBlockRequest { ConsultantId = ConsultantId, FromDate = date, ToDate = date, Session = null, Reason = "Leave" },
            null,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.AffectedAppointments.Should().ContainSingle().Which.Id.Should().Be(morning.Id);
        morning.Status.Should().Be(AppointmentStatus.Booked, "bookings are never cancelled automatically");
        await _blocks.Received(1).AddAsync(Arg.Any<ConsultantBlock>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateBlock_StartingInThePast_IsRejected()
    {
        var result = await _sut.CreateBlockAsync(
            new CreateConsultantBlockRequest { ConsultantId = ConsultantId, FromDate = Today.AddDays(-1), ToDate = Today, Reason = "Leave" },
            null,
            CancellationToken.None);

        result.ErrorCode.Should().Be("APPOINTMENTS.DATE_IN_PAST");
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
