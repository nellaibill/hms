using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

internal static class ConsultantMappingExtensions
{
    public static ConsultantResponse ToResponse(this Consultant consultant) => new()
    {
        Id = consultant.Id,
        Name = consultant.Name,
        DepartmentId = consultant.DepartmentId,
        Specialization = consultant.Specialization,
        IsActive = consultant.IsActive,
        Priority = consultant.Priority,
        PhotoUrl = consultant.PhotoUrl,
        AvailableDays = consultant.AvailableDays,
        VisitStartTime = consultant.VisitStartTime,
        VisitEndTime = consultant.VisitEndTime,
        VisitStartTime2 = consultant.VisitStartTime2,
        VisitEndTime2 = consultant.VisitEndTime2,
        ConsultationTypeCharges = consultant.ConsultationTypes
            .Select(ct => new ConsultationTypeChargeDto(ct.ConsultationTypeId, ct.ConsultantCharge))
            .ToList(),
        CreatedAt = consultant.CreatedAt,
        UpdatedAt = consultant.UpdatedAt,
    };
}
