namespace HMS.Modules.Appointments.Application.Abstractions;

internal interface IAppointmentNumberGenerator
{
    /// <summary>The next human-readable appointment number, e.g. "APT-2026-000001".</summary>
    Task<string> NextAsync(CancellationToken cancellationToken);
}
