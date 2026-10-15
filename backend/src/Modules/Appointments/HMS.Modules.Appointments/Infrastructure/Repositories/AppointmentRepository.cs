using HMS.Modules.Appointments.Application.Abstractions;
using HMS.Modules.Appointments.Contracts;
using HMS.Modules.Appointments.Domain;
using HMS.Modules.Appointments.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HMS.Modules.Appointments.Infrastructure.Repositories;

internal class AppointmentRepository : IAppointmentRepository
{
    private readonly AppointmentsDbContext _dbContext;

    public AppointmentRepository(AppointmentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken)
        => await _dbContext.Appointments.AddAsync(appointment, cancellationToken);

    public Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Appointments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<int>> GetTakenSeatsAsync(Guid consultantId, DateOnly date, TimeOnly startTime, Guid? excludingId, CancellationToken cancellationToken)
        => await HoldingSeat()
            .Where(a => a.ConsultantId == consultantId && a.AppointmentDate == date && a.StartTime == startTime && a.Id != excludingId)
            .Select(a => a.SeatNumber)
            .ToListAsync(cancellationToken);

    public Task<bool> HasBookingForSameDayAsync(Guid consultantId, DateOnly date, Guid? patientId, string phoneNumber, string patientName, Guid? excludingId, CancellationToken cancellationToken)
    {
        var sameDay = _dbContext.Appointments.Where(a =>
            a.Status == AppointmentStatus.Booked && a.ConsultantId == consultantId && a.AppointmentDate == date && a.Id != excludingId);

        if (patientId.HasValue)
        {
            return sameDay.AnyAsync(a => a.PatientId == patientId, cancellationToken);
        }

        var name = patientName.Trim().ToLower();
        return sameDay.AnyAsync(a => a.PatientId == null && a.PhoneNumber == phoneNumber && a.PatientName.ToLower() == name, cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetSeatHoldersForDayAsync(Guid consultantId, DateOnly date, CancellationToken cancellationToken)
        => await HoldingSeat()
            .AsNoTracking()
            .Where(a => a.ConsultantId == consultantId && a.AppointmentDate == date)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Appointment>> GetBookedInRangeAsync(Guid consultantId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
        => await _dbContext.Appointments
            .AsNoTracking()
            .Where(a => a.Status == AppointmentStatus.Booked && a.ConsultantId == consultantId && a.AppointmentDate >= fromDate && a.AppointmentDate <= toDate)
            .OrderBy(a => a.AppointmentDate).ThenBy(a => a.StartTime).ThenBy(a => a.SeatNumber)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Appointment> Items, int TotalCount)> GetPagedAsync(AppointmentListQuery query, CancellationToken cancellationToken)
    {
        var appointments = _dbContext.Appointments.AsNoTracking();

        if (query.FromDate.HasValue)
        {
            appointments = appointments.Where(a => a.AppointmentDate >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            appointments = appointments.Where(a => a.AppointmentDate <= query.ToDate.Value);
        }

        if (query.ConsultantId.HasValue)
        {
            appointments = appointments.Where(a => a.ConsultantId == query.ConsultantId.Value);
        }

        if (query.DepartmentId.HasValue)
        {
            appointments = appointments.Where(a => a.DepartmentId == query.DepartmentId.Value);
        }

        if (query.PatientId.HasValue)
        {
            appointments = appointments.Where(a => a.PatientId == query.PatientId.Value);
        }

        if (query.Status.HasValue)
        {
            appointments = appointments.Where(a => a.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            appointments = appointments.Where(a =>
                EF.Functions.ILike(a.PatientName, term)
                || EF.Functions.ILike(a.PhoneNumber, term)
                || (a.Uhid != null && EF.Functions.ILike(a.Uhid, term))
                || EF.Functions.ILike(a.AppointmentNumber, term));
        }

        appointments = ApplySort(appointments, query.Sort);

        var totalCount = await appointments.CountAsync(cancellationToken);
        var items = await appointments.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyDictionary<AppointmentStatus, int>> CountByStatusAsync(DateOnly date, Guid? consultantId, Guid? departmentId, CancellationToken cancellationToken)
    {
        var appointments = _dbContext.Appointments.Where(a => a.AppointmentDate == date);

        if (consultantId.HasValue)
        {
            appointments = appointments.Where(a => a.ConsultantId == consultantId.Value);
        }

        if (departmentId.HasValue)
        {
            appointments = appointments.Where(a => a.DepartmentId == departmentId.Value);
        }

        var counts = await appointments
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(c => c.Status, c => c.Count);
    }

    public async Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: AppointmentConfiguration.SeatIndexName })
        {
            return false;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<Appointment> HoldingSeat()
        => _dbContext.Appointments.Where(a => a.Status == AppointmentStatus.Booked || a.Status == AppointmentStatus.Arrived);

    // Default: the day in time order — date, slot, then seat (which is token order).
    private static IQueryable<Appointment> ApplySort(IQueryable<Appointment> appointments, string? sort) => sort switch
    {
        "-date" => appointments.OrderByDescending(a => a.AppointmentDate).ThenByDescending(a => a.StartTime).ThenByDescending(a => a.SeatNumber),
        "createdAt" => appointments.OrderBy(a => a.CreatedAt),
        "-createdAt" => appointments.OrderByDescending(a => a.CreatedAt),
        "patientName" => appointments.OrderBy(a => a.PatientName).ThenBy(a => a.AppointmentDate).ThenBy(a => a.StartTime),
        _ => appointments.OrderBy(a => a.AppointmentDate).ThenBy(a => a.StartTime).ThenBy(a => a.SeatNumber),
    };
}
