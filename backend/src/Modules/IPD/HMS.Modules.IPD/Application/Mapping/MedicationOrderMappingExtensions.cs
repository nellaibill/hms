using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;

namespace HMS.Modules.IPD.Application.Mapping;

internal static class MedicationOrderMappingExtensions
{
    public static MedicationOrderResponse ToResponse(this MedicationOrder order) => new()
    {
        Id = order.Id,
        AdmissionId = order.AdmissionId,
        DrugName = order.DrugName,
        Dose = order.Dose,
        Route = order.Route,
        Frequency = order.Frequency,
        StartDate = order.StartDate,
        EndDate = order.EndDate,
        Instructions = order.Instructions,
        OrderedAt = order.OrderedAt,
        OrderedByUserId = order.OrderedByUserId,
        Status = order.Status,
        DiscontinuedAt = order.DiscontinuedAt,
        DiscontinuedReason = order.DiscontinuedReason,
        CreatedAt = order.CreatedAt,
    };
}
