namespace HMS.Modules.IPD.Contracts;

/// <summary>One test/package a ward doctor is placing on an admission. Exactly one of
/// ServiceId/PackageId is expected (validated by CreatePlaceLabOrderRequestValidator) — mirrors
/// HMS.Modules.Laboratory.Contracts.CreateLabOrderLineFromAdmissionRequest, which this maps
/// onto in IPDLabOrderService.</summary>
public record PlaceLabOrderLineRequest
{
    public Guid? ServiceId { get; init; }
    public Guid? PackageId { get; init; }
}

/// <summary>Placed via IPDLabOrdersController — orchestrates a real
/// HMS.Modules.Laboratory.LabOrder (no invoice required) plus one AdmissionCharge per line so
/// the cost is visible on the Charges tab even though IPD has no real billing/invoicing yet.
/// See Application/IPDLabOrderService.cs.</summary>
public record CreatePlaceLabOrderRequest
{
    public IReadOnlyList<PlaceLabOrderLineRequest> Lines { get; init; } = [];
}
