using HMS.Modules.Appointments.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Appointments.Infrastructure;

/// <summary>
/// Formats a short, human-readable appointment number from a real Postgres sequence —
/// coordination-free under concurrent booking, unlike a MAX(...)+1 query. Mirrors
/// Laboratory's LabOrderNumberGenerator.
/// </summary>
internal class AppointmentNumberGenerator : IAppointmentNumberGenerator
{
    private readonly AppointmentsDbContext _dbContext;

    public AppointmentNumberGenerator(AppointmentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> NextAsync(CancellationToken cancellationToken)
    {
        var fullyQualifiedName = $"{AppointmentsDbContext.SchemaName}.{AppointmentsDbContext.AppointmentNumberSequenceName}";
        var results = await _dbContext.Database
            .SqlQuery<long>($"SELECT nextval({fullyQualifiedName}::regclass)")
            .ToListAsync(cancellationToken);

        return $"APT-{DateTime.UtcNow:yyyy}-{results[0]:D6}";
    }
}
