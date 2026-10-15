using System.Globalization;
using HMS.Modules.ActivityLog.Application;
using HMS.Modules.ActivityLog.Contracts;
using HMS.Modules.Appointments.Application.Abstractions;
using HMS.Modules.Appointments.Application.Mapping;
using HMS.Modules.Appointments.Contracts;
using HMS.Modules.Appointments.Domain;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Patients.Application;
using HMS.Shared.Kernel;

namespace HMS.Modules.Appointments.Application;

/// <summary>
/// Booking rules. A booking must land on one of the consultant's slots for that day (built by
/// <see cref="SlotPlanner"/> from the consultant master), on today or a later date within
/// <see cref="MaxDaysAhead"/>, in a slot that hasn't passed, isn't closed by a
/// <see cref="ConsultantBlock"/>, and still has a free seat. The seat check here gives a clear
/// message in the normal case; the database's unique index on the seat is what actually holds
/// when two desks book the same seat at the same moment (see TrySaveChangesAsync).
/// </summary>
internal sealed class AppointmentService : IAppointmentService
{
    public const int MaxDaysAhead = 90;

    /// <summary>Slot length when the consultant master leaves it blank. Matches Masters'
    /// Consultant.DefaultAppointmentSlotMinutes.</summary>
    public const int DefaultSlotMinutes = 15;

    private const int MaxBlockRows = 200;
    private const string EntityType = "Appointment";
    private const string BlockEntityType = "ConsultantBlock";

    private readonly IAppointmentRepository _appointments;
    private readonly IConsultantBlockRepository _blocks;
    private readonly IAppointmentNumberGenerator _numbers;
    private readonly IConsultantService _consultants;
    private readonly IDepartmentService _departments;
    private readonly IAppointmentTypeService _appointmentTypes;
    private readonly IPatientService _patients;
    private readonly IPatientVisitService _visits;
    private readonly IActivityLogService _activityLog;
    private readonly AppointmentClock _clock;

    public AppointmentService(
        IAppointmentRepository appointments,
        IConsultantBlockRepository blocks,
        IAppointmentNumberGenerator numbers,
        IConsultantService consultants,
        IDepartmentService departments,
        IAppointmentTypeService appointmentTypes,
        IPatientService patients,
        IPatientVisitService visits,
        IActivityLogService activityLog,
        AppointmentClock clock)
    {
        _appointments = appointments;
        _blocks = blocks;
        _numbers = numbers;
        _consultants = consultants;
        _departments = departments;
        _appointmentTypes = appointmentTypes;
        _patients = patients;
        _visits = visits;
        _activityLog = activityLog;
        _clock = clock;
    }

    public async Task<Result<AppointmentResponse>> BookAsync(BookAppointmentRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var consultantResult = await GetActiveConsultantAsync(request.ConsultantId, cancellationToken);
        if (!consultantResult.IsSuccess)
        {
            return Fail<AppointmentResponse>(consultantResult);
        }

        var consultant = consultantResult.Value!;

        if (!(await _departments.GetByIdAsync(request.DepartmentId, cancellationToken)).IsSuccess)
        {
            return Result<AppointmentResponse>.Failure(AppointmentErrorCodes.InvalidDepartment, $"Department '{request.DepartmentId}' was not found.");
        }

        if (request.AppointmentTypeId is { } appointmentTypeId && !(await _appointmentTypes.GetByIdAsync(appointmentTypeId, cancellationToken)).IsSuccess)
        {
            return Result<AppointmentResponse>.Failure(AppointmentErrorCodes.InvalidAppointmentType, $"Appointment type '{appointmentTypeId}' was not found.");
        }

        AppointmentPatient patient;
        if (request.PatientId is { } patientId)
        {
            var patientResult = await _patients.GetByIdAsync(patientId, cancellationToken);
            if (!patientResult.IsSuccess)
            {
                return Result<AppointmentResponse>.Failure(AppointmentErrorCodes.InvalidPatient, $"Patient '{patientId}' was not found.");
            }

            var record = patientResult.Value!;
            patient = new AppointmentPatient(record.Id, FullName(record.FirstName, record.LastName), record.PrimaryPhone, record.Uhid);
        }
        else
        {
            patient = new AppointmentPatient(null, request.PatientName!.Trim(), request.PhoneNumber!.Trim(), null);
        }

        if (await _appointments.HasBookingForSameDayAsync(consultant.Id, request.AppointmentDate, patient.PatientId, patient.PhoneNumber, patient.Name, null, cancellationToken))
        {
            return Result<AppointmentResponse>.Failure(
                AppointmentErrorCodes.DuplicateBooking,
                $"{patient.Name} already has an appointment with {consultant.Name} on {FormatDate(request.AppointmentDate)}.");
        }

        var slotResult = await AssignSlotAsync(consultant, request.AppointmentDate, request.StartTime, null, cancellationToken);
        if (!slotResult.IsSuccess)
        {
            return Fail<AppointmentResponse>(slotResult);
        }

        var appointment = Appointment.Book(
            await _numbers.NextAsync(cancellationToken),
            patient,
            request.DepartmentId,
            consultant.Id,
            request.AppointmentTypeId,
            slotResult.Value!,
            request.Source,
            request.Notes,
            actorId);

        await _appointments.AddAsync(appointment, cancellationToken);
        if (!await _appointments.TrySaveChangesAsync(cancellationToken))
        {
            return SlotJustTaken<AppointmentResponse>();
        }

        var response = appointment.ToResponse();
        await LogAsync(
            ActivityLogActions.Create,
            appointment,
            $"Booked appointment {appointment.AppointmentNumber} for {appointment.PatientName} with {consultant.Name} on {FormatSlot(appointment)}",
            null,
            response,
            actorId,
            cancellationToken);

        return Result<AppointmentResponse>.Success(response);
    }

    public async Task<Result<AppointmentResponse>> RescheduleAsync(Guid id, RescheduleAppointmentRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var appointment = await _appointments.GetByIdAsync(id, cancellationToken);
        if (appointment is null)
        {
            return NotFound<AppointmentResponse>(id);
        }

        if (appointment.Status != AppointmentStatus.Booked)
        {
            return InvalidStatus<AppointmentResponse>(appointment, "rescheduled");
        }

        if (appointment.AppointmentDate == request.AppointmentDate && appointment.StartTime == request.StartTime)
        {
            return Result<AppointmentResponse>.Success(appointment.ToResponse());
        }

        var consultantResult = await GetActiveConsultantAsync(appointment.ConsultantId, cancellationToken);
        if (!consultantResult.IsSuccess)
        {
            return Fail<AppointmentResponse>(consultantResult);
        }

        var consultant = consultantResult.Value!;

        if (request.AppointmentDate != appointment.AppointmentDate
            && await _appointments.HasBookingForSameDayAsync(consultant.Id, request.AppointmentDate, appointment.PatientId, appointment.PhoneNumber, appointment.PatientName, appointment.Id, cancellationToken))
        {
            return Result<AppointmentResponse>.Failure(
                AppointmentErrorCodes.DuplicateBooking,
                $"{appointment.PatientName} already has an appointment with {consultant.Name} on {FormatDate(request.AppointmentDate)}.");
        }

        var slotResult = await AssignSlotAsync(consultant, request.AppointmentDate, request.StartTime, appointment.Id, cancellationToken);
        if (!slotResult.IsSuccess)
        {
            return Fail<AppointmentResponse>(slotResult);
        }

        var before = appointment.ToResponse();
        var from = FormatSlot(appointment);
        appointment.Reschedule(slotResult.Value!, actorId);

        if (!await _appointments.TrySaveChangesAsync(cancellationToken))
        {
            return SlotJustTaken<AppointmentResponse>();
        }

        var after = appointment.ToResponse();
        await LogAsync(
            ActivityLogActions.Update,
            appointment,
            $"Rescheduled appointment {appointment.AppointmentNumber} from {from} to {FormatSlot(appointment)}",
            before,
            after,
            actorId,
            cancellationToken);

        return Result<AppointmentResponse>.Success(after);
    }

    public async Task<Result<AppointmentResponse>> CancelAsync(Guid id, CancelAppointmentRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var appointment = await _appointments.GetByIdAsync(id, cancellationToken);
        if (appointment is null)
        {
            return NotFound<AppointmentResponse>(id);
        }

        if (appointment.Status != AppointmentStatus.Booked)
        {
            return InvalidStatus<AppointmentResponse>(appointment, "cancelled");
        }

        var before = appointment.ToResponse();
        appointment.Cancel(request.Reason, actorId);
        await _appointments.SaveChangesAsync(cancellationToken);

        var after = appointment.ToResponse();
        await LogAsync(ActivityLogActions.Update, appointment, $"Cancelled appointment {appointment.AppointmentNumber}: {appointment.CancelReason}", before, after, actorId, cancellationToken);

        return Result<AppointmentResponse>.Success(after);
    }

    public async Task<Result<AppointmentResponse>> MarkNoShowAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var appointment = await _appointments.GetByIdAsync(id, cancellationToken);
        if (appointment is null)
        {
            return NotFound<AppointmentResponse>(id);
        }

        if (appointment.Status != AppointmentStatus.Booked)
        {
            return InvalidStatus<AppointmentResponse>(appointment, "marked as a no-show");
        }

        if (appointment.AppointmentDate > _clock.Today)
        {
            return Result<AppointmentResponse>.Failure(
                AppointmentErrorCodes.WrongDay,
                $"This appointment is on {FormatDate(appointment.AppointmentDate)}. It can be marked as a no-show on that day or later.");
        }

        var before = appointment.ToResponse();
        appointment.MarkNoShow(actorId);
        await _appointments.SaveChangesAsync(cancellationToken);

        var after = appointment.ToResponse();
        await LogAsync(ActivityLogActions.Update, appointment, $"Marked appointment {appointment.AppointmentNumber} as a no-show", before, after, actorId, cancellationToken);

        return Result<AppointmentResponse>.Success(after);
    }

    public async Task<Result<AppointmentResponse>> MarkArrivedAsync(Guid id, MarkAppointmentArrivedRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var appointment = await _appointments.GetByIdAsync(id, cancellationToken);
        if (appointment is null)
        {
            return NotFound<AppointmentResponse>(id);
        }

        if (appointment.Status != AppointmentStatus.Booked)
        {
            return InvalidStatus<AppointmentResponse>(appointment, "checked in");
        }

        if (appointment.AppointmentDate != _clock.Today)
        {
            return Result<AppointmentResponse>.Failure(
                AppointmentErrorCodes.WrongDay,
                $"This appointment is on {FormatDate(appointment.AppointmentDate)}. Reschedule it to today before checking the patient in.");
        }

        if (appointment.PatientId is { } bookedPatientId && bookedPatientId != request.PatientId)
        {
            return Result<AppointmentResponse>.Failure(AppointmentErrorCodes.PatientMismatch, "This appointment was booked for a different patient.");
        }

        var patientResult = await _patients.GetByIdAsync(request.PatientId, cancellationToken);
        if (!patientResult.IsSuccess)
        {
            return Result<AppointmentResponse>.Failure(AppointmentErrorCodes.InvalidPatient, $"Patient '{request.PatientId}' was not found.");
        }

        if (!(await _visits.GetByIdAsync(request.PatientId, request.VisitId, cancellationToken)).IsSuccess)
        {
            return Result<AppointmentResponse>.Failure(AppointmentErrorCodes.InvalidVisit, $"Visit '{request.VisitId}' was not found for this patient.");
        }

        var record = patientResult.Value!;
        var before = appointment.ToResponse();
        appointment.MarkArrived(new AppointmentPatient(record.Id, FullName(record.FirstName, record.LastName), record.PrimaryPhone, record.Uhid), request.VisitId, actorId);
        await _appointments.SaveChangesAsync(cancellationToken);

        var after = appointment.ToResponse();
        await LogAsync(ActivityLogActions.Update, appointment, $"Checked in appointment {appointment.AppointmentNumber} ({appointment.PatientName}, UHID {appointment.Uhid})", before, after, actorId, cancellationToken);

        return Result<AppointmentResponse>.Success(after);
    }

    public async Task<Result<AppointmentResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var appointment = await _appointments.GetByIdAsync(id, cancellationToken);
        return appointment is null ? NotFound<AppointmentResponse>(id) : Result<AppointmentResponse>.Success(appointment.ToResponse());
    }

    public async Task<PagedResult<AppointmentResponse>> GetPagedAsync(AppointmentListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _appointments.GetPagedAsync(query, cancellationToken);
        return new PagedResult<AppointmentResponse>(items.Select(a => a.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<Result<AppointmentSlotsResponse>> GetSlotsAsync(AppointmentSlotsQuery query, CancellationToken cancellationToken)
    {
        var consultantResult = await _consultants.GetByIdAsync(query.ConsultantId, cancellationToken);
        if (!consultantResult.IsSuccess)
        {
            return Result<AppointmentSlotsResponse>.Failure(AppointmentErrorCodes.InvalidConsultant, $"Consultant '{query.ConsultantId}' was not found.");
        }

        var consultant = consultantResult.Value!;
        var schedule = ToSchedule(consultant);
        var empty = new AppointmentSlotsResponse
        {
            ConsultantId = consultant.Id,
            Date = query.Date,
            SlotMinutes = schedule.SlotMinutes,
            PatientsPerSlot = schedule.PatientsPerSlot,
        };

        if (!consultant.IsActive)
        {
            return Result<AppointmentSlotsResponse>.Success(empty with { Message = $"{consultant.Name} is marked inactive in the Consultant master." });
        }

        var planned = SlotPlanner.PlanDay(schedule, query.Date);
        if (planned.Count == 0)
        {
            return Result<AppointmentSlotsResponse>.Success(empty with { Message = NoSlotsMessage(consultant, schedule, query.Date) });
        }

        var holders = await _appointments.GetSeatHoldersForDayAsync(consultant.Id, query.Date, cancellationToken);
        var blocks = await _blocks.GetOverlappingAsync(consultant.Id, query.Date, query.Date, cancellationToken);
        var today = _clock.Today;
        var now = _clock.TimeOfDay;
        var withinWindow = query.Date >= today && query.Date <= today.AddDays(MaxDaysAhead);

        var slots = planned.Select(slot =>
        {
            var booked = holders.Count(h => h.StartTime == slot.Start);
            var available = Math.Max(0, schedule.PatientsPerSlot - booked);
            var block = blocks.FirstOrDefault(b => b.Covers(query.Date, slot.Session));
            var isPast = query.Date < today || (query.Date == today && slot.End <= now);

            return new AppointmentSlotResponse
            {
                Session = slot.Session,
                StartTime = slot.Start,
                EndTime = slot.End,
                FirstToken = slot.FirstToken(schedule.PatientsPerSlot),
                Capacity = schedule.PatientsPerSlot,
                Booked = booked,
                Available = available,
                IsBlocked = block is not null,
                BlockReason = block?.Reason,
                IsPast = isPast,
                IsBookable = withinWindow && block is null && !isPast && available > 0,
            };
        }).ToList();

        var dayBlock = slots.All(s => s.IsBlocked) ? blocks.FirstOrDefault(b => b.Session is null) ?? blocks.FirstOrDefault() : null;

        return Result<AppointmentSlotsResponse>.Success(empty with
        {
            IsWorkingDay = true,
            Message = dayBlock is null ? null : $"{consultant.Name} is not available: {dayBlock.Reason}",
            Slots = slots,
        });
    }

    public async Task<AppointmentSummaryResponse> GetSummaryAsync(AppointmentSummaryQuery query, CancellationToken cancellationToken)
    {
        var counts = await _appointments.CountByStatusAsync(query.Date, query.ConsultantId, query.DepartmentId, cancellationToken);
        int Count(AppointmentStatus status) => counts.TryGetValue(status, out var value) ? value : 0;

        return new AppointmentSummaryResponse
        {
            Date = query.Date,
            Total = counts.Values.Sum(),
            Booked = Count(AppointmentStatus.Booked),
            Arrived = Count(AppointmentStatus.Arrived),
            Cancelled = Count(AppointmentStatus.Cancelled),
            NoShow = Count(AppointmentStatus.NoShow),
        };
    }

    public async Task<Result<ConsultantBlockCreatedResponse>> CreateBlockAsync(CreateConsultantBlockRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var consultantResult = await _consultants.GetByIdAsync(request.ConsultantId, cancellationToken);
        if (!consultantResult.IsSuccess)
        {
            return Result<ConsultantBlockCreatedResponse>.Failure(AppointmentErrorCodes.InvalidConsultant, $"Consultant '{request.ConsultantId}' was not found.");
        }

        if (request.FromDate < _clock.Today)
        {
            return Result<ConsultantBlockCreatedResponse>.Failure(AppointmentErrorCodes.DateInPast, "Leave can be blocked from today onwards.");
        }

        var block = ConsultantBlock.Create(request.ConsultantId, request.FromDate, request.ToDate, request.Session, request.Reason, actorId);
        await _blocks.AddAsync(block, cancellationToken);
        await _blocks.SaveChangesAsync(cancellationToken);

        var affected = (await _appointments.GetBookedInRangeAsync(request.ConsultantId, request.FromDate, request.ToDate, cancellationToken))
            .Where(a => block.Covers(a.AppointmentDate, a.Session))
            .Select(a => a.ToResponse())
            .ToList();

        var response = block.ToResponse();
        await _activityLog.LogAsync(new ActivityLogRequest
        {
            Action = ActivityLogActions.Create,
            Module = ActivityLogModules.Appointments,
            EntityType = BlockEntityType,
            EntityId = block.Id.ToString(),
            Description = $"Blocked {consultantResult.Value!.Name} from {FormatDate(block.FromDate)} to {FormatDate(block.ToDate)}{SessionLabel(block.Session)}: {block.Reason}",
            NewValues = response,
            UserId = actorId,
        }, cancellationToken);

        return Result<ConsultantBlockCreatedResponse>.Success(new ConsultantBlockCreatedResponse { Block = response, AffectedAppointments = affected });
    }

    public async Task<Result> RemoveBlockAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var block = await _blocks.GetByIdAsync(id, cancellationToken);
        if (block is null)
        {
            return Result.Failure(AppointmentErrorCodes.BlockNotFound, $"Leave block '{id}' was not found.");
        }

        var before = block.ToResponse();
        block.SoftDelete(actorId);
        await _blocks.SaveChangesAsync(cancellationToken);

        await _activityLog.LogAsync(new ActivityLogRequest
        {
            Action = ActivityLogActions.Delete,
            Module = ActivityLogModules.Appointments,
            EntityType = BlockEntityType,
            EntityId = block.Id.ToString(),
            Description = $"Removed leave block from {FormatDate(block.FromDate)} to {FormatDate(block.ToDate)}{SessionLabel(block.Session)}",
            OldValues = before,
            UserId = actorId,
        }, cancellationToken);

        return Result.Success();
    }

    public async Task<IReadOnlyList<ConsultantBlockResponse>> GetBlocksAsync(ConsultantBlockListQuery query, CancellationToken cancellationToken)
        => (await _blocks.ListAsync(query, MaxBlockRows, cancellationToken)).Select(b => b.ToResponse()).ToList();

    internal static ConsultantSchedule ToSchedule(ConsultantResponse consultant)
    {
        var days = consultant.AvailableDays
            .Select(day => Enum.TryParse<DayOfWeek>(day, out var parsed) ? parsed : (DayOfWeek?)null)
            .OfType<DayOfWeek>()
            .ToHashSet();

        var sessions = new List<ScheduleSession>();
        if (consultant.VisitStartTime is { } start1 && consultant.VisitEndTime is { } end1)
        {
            sessions.Add(new ScheduleSession(1, start1, end1));
        }

        if (consultant.VisitStartTime2 is { } start2 && consultant.VisitEndTime2 is { } end2)
        {
            sessions.Add(new ScheduleSession(2, start2, end2));
        }

        return new ConsultantSchedule(days, sessions, consultant.AppointmentSlotMinutes ?? DefaultSlotMinutes, consultant.PatientsPerSlot ?? 1);
    }

    private async Task<Result<SlotAssignment>> AssignSlotAsync(ConsultantResponse consultant, DateOnly date, TimeOnly startTime, Guid? excludingId, CancellationToken cancellationToken)
    {
        var today = _clock.Today;
        if (date < today)
        {
            return Result<SlotAssignment>.Failure(AppointmentErrorCodes.DateInPast, "Pick today or a later date.");
        }

        if (date > today.AddDays(MaxDaysAhead))
        {
            return Result<SlotAssignment>.Failure(AppointmentErrorCodes.TooFarAhead, $"Appointments can be booked up to {MaxDaysAhead} days ahead.");
        }

        var schedule = ToSchedule(consultant);
        var planned = SlotPlanner.PlanDay(schedule, date);
        if (planned.Count == 0)
        {
            return Result<SlotAssignment>.Failure(AppointmentErrorCodes.NotAClinicDay, NoSlotsMessage(consultant, schedule, date));
        }

        var slot = planned.FirstOrDefault(s => s.Start == startTime);
        if (slot is null)
        {
            return Result<SlotAssignment>.Failure(
                AppointmentErrorCodes.InvalidSlot,
                $"{FormatTime(startTime)} is not one of {consultant.Name}'s slots on {FormatDate(date)}.");
        }

        if (date == today && slot.End <= _clock.TimeOfDay)
        {
            return Result<SlotAssignment>.Failure(AppointmentErrorCodes.SlotInPast, $"The {FormatTime(slot.Start)} slot has already passed.");
        }

        var blocks = await _blocks.GetOverlappingAsync(consultant.Id, date, date, cancellationToken);
        var block = blocks.FirstOrDefault(b => b.Covers(date, slot.Session));
        if (block is not null)
        {
            return Result<SlotAssignment>.Failure(AppointmentErrorCodes.ConsultantUnavailable, $"{consultant.Name} is not available then: {block.Reason}");
        }

        var takenSeats = await _appointments.GetTakenSeatsAsync(consultant.Id, date, slot.Start, excludingId, cancellationToken);
        var seat = SlotPlanner.FirstFreeSeat(takenSeats, schedule.PatientsPerSlot);
        if (seat is null)
        {
            return Result<SlotAssignment>.Failure(AppointmentErrorCodes.SlotFull, $"The {FormatTime(slot.Start)} slot is full. Pick another time.");
        }

        return Result<SlotAssignment>.Success(new SlotAssignment(
            date,
            slot.Start,
            slot.End,
            slot.Session,
            seat.Value,
            slot.TokenFor(seat.Value, schedule.PatientsPerSlot)));
    }

    private async Task<Result<ConsultantResponse>> GetActiveConsultantAsync(Guid consultantId, CancellationToken cancellationToken)
    {
        var result = await _consultants.GetByIdAsync(consultantId, cancellationToken);
        if (!result.IsSuccess)
        {
            return Result<ConsultantResponse>.Failure(AppointmentErrorCodes.InvalidConsultant, $"Consultant '{consultantId}' was not found.");
        }

        return result.Value!.IsActive
            ? result
            : Result<ConsultantResponse>.Failure(AppointmentErrorCodes.ConsultantInactive, $"{result.Value.Name} is marked inactive and can't be booked.");
    }

    private static string NoSlotsMessage(ConsultantResponse consultant, ConsultantSchedule schedule, DateOnly date)
    {
        if (schedule.Sessions.Count == 0 || schedule.Days.Count == 0)
        {
            return $"{consultant.Name} has no clinic days or visiting hours set. Add them in the Consultant master.";
        }

        if (!schedule.Days.Contains(date.DayOfWeek))
        {
            return $"{consultant.Name} doesn't hold clinic on {date.DayOfWeek}s.";
        }

        return $"{consultant.Name}'s visiting hours are shorter than one {schedule.SlotMinutes}-minute slot.";
    }

    private Task LogAsync(string action, Appointment appointment, string description, object? oldValues, object? newValues, Guid? actorId, CancellationToken cancellationToken)
        => _activityLog.LogAsync(new ActivityLogRequest
        {
            Action = action,
            Module = ActivityLogModules.Appointments,
            EntityType = EntityType,
            EntityId = appointment.Id.ToString(),
            Description = description,
            OldValues = oldValues,
            NewValues = newValues,
            UserId = actorId,
        }, cancellationToken);

    private static string FullName(string firstName, string lastName) => $"{firstName} {lastName}".Trim();

    private static string FormatDate(DateOnly date) => date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

    private static string FormatTime(TimeOnly time) => time.ToString("HH:mm", CultureInfo.InvariantCulture);

    private static string FormatSlot(Appointment appointment) => $"{FormatDate(appointment.AppointmentDate)} {FormatTime(appointment.StartTime)}";

    private static string SessionLabel(int? session) => session is null ? string.Empty : $" (session {session})";

    private static Result<T> Fail<T>(Result failure) => Result<T>.Failure(failure.ErrorCode!, failure.Error!);

    private static Result<T> NotFound<T>(Guid id) => Result<T>.Failure(AppointmentErrorCodes.NotFound, $"Appointment '{id}' was not found.");

    private static Result<T> InvalidStatus<T>(Appointment appointment, string pastTenseAction)
        => Result<T>.Failure(AppointmentErrorCodes.InvalidStatus, $"Appointment {appointment.AppointmentNumber} is {appointment.Status} and can't be {pastTenseAction}.");

    private static Result<T> SlotJustTaken<T>()
        => Result<T>.Failure(AppointmentErrorCodes.SlotFull, "Someone else just booked that slot. Pick another time.");
}
