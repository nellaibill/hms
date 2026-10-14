using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Contracts;

// ---- OPD prescriptions at the pharmacy -------------------------------------------------

/// <summary>One drug line as the doctor wrote it — free text, not a catalog item.</summary>
public record PrescribedDrugResponse
{
    public string DrugName { get; init; } = string.Empty;
    public string? Dose { get; init; }
    public string? Route { get; init; }
    public string? Frequency { get; init; }
    public int? DurationDays { get; init; }
    public string? Instructions { get; init; }

    /// <summary>Units the course works out to (doses a day × days), when the frequency and
    /// duration can be read. Null when they cannot — the pharmacist decides.</summary>
    public decimal? SuggestedQuantity { get; init; }
}

/// <summary>A bill already made against a prescription.</summary>
public record PrescriptionBillResponse
{
    public Guid SaleId { get; init; }
    public string BillNo { get; init; } = string.Empty;
    public decimal NetAmount { get; init; }
}

/// <summary>A completed OPD consultation with medicines prescribed, as the pharmacy sees it.</summary>
public record PrescriptionQueueItemResponse
{
    public Guid ConsultationId { get; init; }
    public Guid PatientId { get; init; }
    public string Uhid { get; init; } = string.Empty;
    public string PatientName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string DoctorName { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;
    public DateTime PrescribedAt { get; init; }
    public IReadOnlyList<PrescribedDrugResponse> Drugs { get; init; } = [];

    /// <summary>Bills already made against it. Empty means it is still waiting.</summary>
    public IReadOnlyList<PrescriptionBillResponse> Bills { get; init; } = [];
}

// ---- IP indents ------------------------------------------------------------------------

public enum IndentStatus
{
    Pending,
    PartlyIssued,
    Issued,
    Cancelled,
}

public record IndentLineRequest
{
    public Guid ProductId { get; init; }

    /// <summary>Units.</summary>
    public decimal Quantity { get; init; }
    public string? Instructions { get; init; }
}

public record CreateIndentRequest
{
    /// <summary>The pharmacy to supply it. Omitted, the default store.</summary>
    public Guid? StoreId { get; init; }
    public Guid AdmissionId { get; init; }
    public bool IsUrgent { get; init; }
    public string? Remarks { get; init; }
    public IReadOnlyList<IndentLineRequest> Lines { get; init; } = [];
}

public record IndentLineResponse
{
    public Guid Id { get; init; }
    public int LineNo { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal IssuedQuantity { get; init; }
    public decimal PendingQuantity { get; init; }
    public string? Instructions { get; init; }
}

public record IndentResponse
{
    public Guid Id { get; init; }
    public string IndentNo { get; init; } = string.Empty;
    public Guid StoreId { get; init; }
    public Guid AdmissionId { get; init; }
    public string AdmissionNumber { get; init; } = string.Empty;
    public Guid PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public string? PatientUhid { get; init; }
    public string? Location { get; init; }
    public string? DoctorName { get; init; }
    public DateTime RequestedAt { get; init; }
    public IndentStatus Status { get; init; }
    public bool IsUrgent { get; init; }
    public string? Remarks { get; init; }
    public int LineCount { get; init; }

    /// <summary>Empty on list responses; filled when a single indent is fetched.</summary>
    public IReadOnlyList<IndentLineResponse> Lines { get; init; } = [];
}

public class IndentListQuery : PagedRequest
{
    public Guid? AdmissionId { get; set; }
    public IndentStatus? Status { get; set; }

    /// <summary>Only indents the pharmacy still has something to issue against.</summary>
    public bool? OpenOnly { get; set; }
}
