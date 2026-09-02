using HMS.Modules.Laboratory.Contracts;
using HMS.Modules.Laboratory.Domain;

namespace HMS.Modules.Laboratory.Application.Mapping;

internal static class LabOrderMappingExtensions
{
    public static ResultParameterResponse ToResponse(this LabResultParameter parameter) => new()
    {
        Id = parameter.Id,
        ParameterName = parameter.ParameterName,
        ResultValue = parameter.ResultValue,
        Unit = parameter.Unit,
        ReferenceRange = parameter.ReferenceRange,
        Flag = parameter.Flag,
        Remarks = parameter.Remarks,
    };

    public static LabOrderItemEventResponse ToResponse(this LabOrderItemEvent orderItemEvent) => new()
    {
        Id = orderItemEvent.Id,
        EventType = orderItemEvent.EventType,
        ActorId = orderItemEvent.ActorId,
        OccurredAt = orderItemEvent.OccurredAt,
        Remarks = orderItemEvent.Remarks,
    };

    public static LabOrderItemResponse ToResponse(this LabOrderItem item) => new()
    {
        Id = item.Id,
        ServiceId = item.ServiceId,
        PackageId = item.PackageId,
        TestName = item.TestName,
        DepartmentId = item.DepartmentId,
        ConsultantId = item.ConsultantId,
        SampleType = item.SampleType,
        Status = item.Status,
        CollectedAt = item.CollectedAt,
        CollectedBy = item.CollectedBy,
        CollectionLocation = item.CollectionLocation,
        SampleQuantity = item.SampleQuantity,
        CollectionRemarks = item.CollectionRemarks,
        RejectionReason = item.RejectionReason,
        RejectionRemarks = item.RejectionRemarks,
        RejectedAt = item.RejectedAt,
        RejectedBy = item.RejectedBy,
        SubmittedForVerificationAt = item.SubmittedForVerificationAt,
        VerifiedAt = item.VerifiedAt,
        VerifiedBy = item.VerifiedBy,
        CorrectionReason = item.CorrectionReason,
        CorrectionRequestedAt = item.CorrectionRequestedAt,
        Parameters = item.Parameters.Select(p => p.ToResponse()).ToList(),
        Events = item.Events.Select(e => e.ToResponse()).ToList(),
    };

    public static LabOrderResponse ToResponse(this LabOrder order) => new()
    {
        Id = order.Id,
        LabOrderNumber = order.LabOrderNumber,
        InvoiceId = order.InvoiceId,
        PatientId = order.PatientId,
        PatientName = order.PatientName,
        PatientUhid = order.PatientUhid,
        VisitId = order.VisitId,
        Source = order.Source,
        Priority = order.Priority,
        OverallStatus = order.OverallStatus,
        CreatedAt = order.CreatedAt,
        ReportGeneratedAt = order.ReportGeneratedAt,
        ReportGeneratedBy = order.ReportGeneratedBy,
        ReportReleasedAt = order.ReportReleasedAt,
        ReportReleasedBy = order.ReportReleasedBy,
        Items = order.Items.Select(i => i.ToResponse()).ToList(),
    };
}
