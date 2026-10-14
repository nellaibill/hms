using HMS.Modules.Pharmacy.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Domain;

/// <summary>
/// A ward's request to the pharmacy for an admitted patient's medicines. It moves no stock
/// and charges nothing: it is the list the pharmacy fills. Filling it is an ordinary
/// pharmacy bill (charged to the admission), and the indent records how much of each item
/// has been issued so far — a request can be filled over more than one bill.
/// </summary>
internal class PharmacyIndent : Entity
{
    private readonly List<PharmacyIndentLine> _lines = [];

    public string IndentNo { get; private set; } = null!;
    public Guid StoreId { get; private set; }
    public Guid AdmissionId { get; private set; }
    public string AdmissionNumber { get; private set; } = null!;
    public Guid PatientId { get; private set; }
    public string PatientName { get; private set; } = null!;
    public string? PatientUhid { get; private set; }

    /// <summary>Ward and bed when the indent was raised — where the medicines are to go.</summary>
    public string? Location { get; private set; }
    public string? DoctorName { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public IndentStatus Status { get; private set; }
    public bool IsUrgent { get; private set; }
    public string? Remarks { get; private set; }

    public IReadOnlyList<PharmacyIndentLine> Lines => _lines;

    /// <summary>The pharmacy can still issue against it.</summary>
    public bool IsOpen => Status is IndentStatus.Pending or IndentStatus.PartlyIssued;

    // Required by EF Core materialization.
    private PharmacyIndent()
    {
    }

    private PharmacyIndent(Guid id, Guid? createdBy)
        : base(id, createdBy)
    {
    }

    public static PharmacyIndent Create(
        string indentNo, Guid storeId, Guid admissionId, string admissionNumber, Guid patientId, string patientName, string? patientUhid,
        string? location, string? doctorName, bool isUrgent, string? remarks, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(indentNo, nameof(indentNo));
        Guard.AgainstNullOrWhiteSpace(patientName, nameof(patientName));

        return new PharmacyIndent(Guid.CreateVersion7(), createdBy)
        {
            IndentNo = indentNo,
            StoreId = storeId,
            AdmissionId = admissionId,
            AdmissionNumber = admissionNumber,
            PatientId = patientId,
            PatientName = patientName.Trim(),
            PatientUhid = Clean(patientUhid),
            Location = Clean(location),
            DoctorName = Clean(doctorName),
            RequestedAt = DateTime.UtcNow,
            Status = IndentStatus.Pending,
            IsUrgent = isUrgent,
            Remarks = Clean(remarks),
        };
    }

    public PharmacyIndentLine AddLine(Guid productId, string productName, decimal quantity, string? instructions)
    {
        Guard.AgainstNonPositive(quantity, nameof(quantity));

        var line = new PharmacyIndentLine(Id, _lines.Count + 1, productId, productName, quantity, Clean(instructions), CreatedBy);
        _lines.Add(line);
        return line;
    }

    /// <summary>Records units of a product issued on a pharmacy bill. A product that was not
    /// asked for is ignored — the pharmacist may add to a bill, and the bill still stands.</summary>
    public void RecordIssue(Guid productId, decimal quantity, Guid? updatedBy)
    {
        if (!IsOpen)
        {
            throw new InvalidOperationException($"Indent {IndentNo} is {Status.ToString().ToLowerInvariant()} and cannot be issued against.");
        }

        var line = _lines.FirstOrDefault(l => l.ProductId == productId && l.PendingQuantity > 0) ?? _lines.FirstOrDefault(l => l.ProductId == productId);
        if (line is null || quantity <= 0)
        {
            return;
        }

        line.RecordIssue(quantity, updatedBy);
        Status = _lines.All(l => l.PendingQuantity <= 0) ? IndentStatus.Issued : IndentStatus.PartlyIssued;
        MarkUpdated(updatedBy);
    }

    /// <summary>Closes what is left: the ward no longer needs it, or it will not be supplied.</summary>
    public void Cancel(Guid? updatedBy)
    {
        if (!IsOpen)
        {
            throw new InvalidOperationException($"Indent {IndentNo} is already {Status.ToString().ToLowerInvariant()}.");
        }

        // A part-issued indent that is closed is finished, not cancelled: what was issued stands.
        Status = Status == IndentStatus.PartlyIssued ? IndentStatus.Issued : IndentStatus.Cancelled;
        MarkUpdated(updatedBy);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

internal class PharmacyIndentLine : Entity
{
    public Guid IndentId { get; private set; }
    public int LineNo { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = null!;

    /// <summary>Units asked for, and units issued so far.</summary>
    public decimal Quantity { get; private set; }
    public decimal IssuedQuantity { get; private set; }
    public string? Instructions { get; private set; }

    public decimal PendingQuantity => Math.Max(Quantity - IssuedQuantity, 0m);

    // Required by EF Core materialization.
    private PharmacyIndentLine()
    {
    }

    internal PharmacyIndentLine(Guid indentId, int lineNo, Guid productId, string productName, decimal quantity, string? instructions, Guid? createdBy)
        : base(Guid.CreateVersion7(), createdBy)
    {
        IndentId = indentId;
        LineNo = lineNo;
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        Instructions = instructions;
    }

    internal void RecordIssue(decimal quantity, Guid? updatedBy)
    {
        IssuedQuantity += quantity;
        MarkUpdated(updatedBy);
    }
}
