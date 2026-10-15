using HMS.Modules.Appointments.Contracts;
using HMS.Modules.Appointments.Domain;

namespace HMS.Modules.Appointments.Application.Mapping;

internal static class AppointmentMappingExtensions
{
    public static AppointmentResponse ToResponse(this Appointment appointment) => new()
    {
        Id = appointment.Id,
        AppointmentNumber = appointment.AppointmentNumber,
        PatientId = appointment.PatientId,
        PatientName = appointment.PatientName,
        PhoneNumber = appointment.PhoneNumber,
        Uhid = appointment.Uhid,
        IsNewPatient = appointment.PatientId is null,
        DepartmentId = appointment.DepartmentId,
        ConsultantId = appointment.ConsultantId,
        AppointmentTypeId = appointment.AppointmentTypeId,
        AppointmentDate = appointment.AppointmentDate,
        StartTime = appointment.StartTime,
        EndTime = appointment.EndTime,
        Session = appointment.Session,
        TokenNumber = appointment.TokenNumber,
        Status = appointment.Status,
        Source = appointment.Source,
        Notes = appointment.Notes,
        CancelReason = appointment.CancelReason,
        CancelledAt = appointment.CancelledAt,
        VisitId = appointment.VisitId,
        ArrivedAt = appointment.ArrivedAt,
        NoShowAt = appointment.NoShowAt,
        RescheduleCount = appointment.RescheduleCount,
        CreatedAt = appointment.CreatedAt,
        CreatedBy = appointment.CreatedBy,
        UpdatedAt = appointment.UpdatedAt,
    };

    public static ConsultantBlockResponse ToResponse(this ConsultantBlock block) => new()
    {
        Id = block.Id,
        ConsultantId = block.ConsultantId,
        FromDate = block.FromDate,
        ToDate = block.ToDate,
        Session = block.Session,
        Reason = block.Reason,
        CreatedAt = block.CreatedAt,
        CreatedBy = block.CreatedBy,
    };
}
