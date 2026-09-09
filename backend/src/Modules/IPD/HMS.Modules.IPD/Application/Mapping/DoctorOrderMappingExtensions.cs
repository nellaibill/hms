using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;

namespace HMS.Modules.IPD.Application.Mapping;

internal static class DoctorOrderMappingExtensions
{
    public static DoctorOrderResponse ToResponse(this DoctorOrder order) => new()
    {
        Id = order.Id,
        AdmissionId = order.AdmissionId,
        OrderType = order.OrderType,
        Description = order.Description,
        Instructions = order.Instructions,
        OrderedAt = order.OrderedAt,
        OrderedByUserId = order.OrderedByUserId,
        Status = order.Status,
        CompletedAt = order.CompletedAt,
        CancelledAt = order.CancelledAt,
        CancellationReason = order.CancellationReason,
        CreatedAt = order.CreatedAt,
    };
}
