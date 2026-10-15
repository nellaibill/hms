using HMS.Modules.Appointments.Contracts;
using HMS.Modules.Appointments.Domain;

namespace HMS.Modules.Appointments.Application.Abstractions;

internal interface IAppointmentRepository
{
    Task AddAsync(Appointment appointment, CancellationToken cancellationToken);

    Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Seat numbers already held (Booked or Arrived) in one slot, optionally ignoring
    /// the appointment being rescheduled.</summary>
    Task<IReadOnlyList<int>> GetTakenSeatsAsync(Guid consultantId, DateOnly date, TimeOnly startTime, Guid? excludingId, CancellationToken cancellationToken);

    /// <summary>Whether the same patient already holds a Booked appointment with this consultant
    /// on this date. A registered patient is matched by PatientId; a first-time caller by phone
    /// number and name together, since family members often share one phone.</summary>
    Task<bool> HasBookingForSameDayAsync(Guid consultantId, DateOnly date, Guid? patientId, string phoneNumber, string patientName, Guid? excludingId, CancellationToken cancellationToken);

    /// <summary>Booked/Arrived appointments of one consultant on one date — the slot grid's
    /// occupancy.</summary>
    Task<IReadOnlyList<Appointment>> GetSeatHoldersForDayAsync(Guid consultantId, DateOnly date, CancellationToken cancellationToken);

    /// <summary>Booked appointments of one consultant between two dates inclusive.</summary>
    Task<IReadOnlyList<Appointment>> GetBookedInRangeAsync(Guid consultantId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Appointment> Items, int TotalCount)> GetPagedAsync(AppointmentListQuery query, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<AppointmentStatus, int>> CountByStatusAsync(DateOnly date, Guid? consultantId, Guid? departmentId, CancellationToken cancellationToken);

    /// <summary>Saves pending changes. Returns false, instead of throwing, when the save lost a
    /// race for the same seat (the slot's unique index rejected it).</summary>
    Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
