using HMS.Modules.Laboratory.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Laboratory.Domain;

/// <summary>
/// One analyte result row on a LabOrderItem — a single-parameter test (e.g. "Blood Grouping")
/// has exactly one row, a panel (e.g. CBC) has one row per analyte. ResultValue is
/// deliberately a string, not a typed numeric column — it needs to cover numeric/text/
/// positive-negative results uniformly, the same "flexibility over strict typing" trade-off
/// InvoiceLineItem's own free-text DepartmentId/ConsultantId already makes in this codebase.
/// ReferenceRange/Unit/Flag only ever hold whatever a human enters — this system has no
/// reference-range configuration data to populate or compute them from, so nothing here ever
/// defaults or auto-computes those three. No standalone mutators: the parent LabOrderItem's
/// SaveResultDraft replaces the whole set each time, mirroring how Invoice.Create builds all
/// of its InvoiceLineItems in one call.
/// </summary>
internal class LabResultParameter : Entity
{
    public Guid LabOrderItemId { get; private set; }
    public string ParameterName { get; private set; } = null!;
    public string ResultValue { get; private set; } = null!;
    public string? Unit { get; private set; }
    public string? ReferenceRange { get; private set; }
    public LabResultFlag? Flag { get; private set; }
    public string? Remarks { get; private set; }

    // Required by EF Core materialization.
    private LabResultParameter()
    {
    }

    private LabResultParameter(
        Guid id,
        Guid labOrderItemId,
        string parameterName,
        string resultValue,
        string? unit,
        string? referenceRange,
        LabResultFlag? flag,
        string? remarks,
        Guid? createdBy)
        : base(id, createdBy)
    {
        LabOrderItemId = labOrderItemId;
        ParameterName = parameterName;
        ResultValue = resultValue;
        Unit = unit;
        ReferenceRange = referenceRange;
        Flag = flag;
        Remarks = remarks;
    }

    public static LabResultParameter Create(
        Guid labOrderItemId,
        string parameterName,
        string resultValue,
        string? unit,
        string? referenceRange,
        LabResultFlag? flag,
        string? remarks,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(parameterName, nameof(parameterName));
        Guard.AgainstNullOrWhiteSpace(resultValue, nameof(resultValue));

        return new LabResultParameter(
            Guid.CreateVersion7(),
            labOrderItemId,
            parameterName.Trim(),
            resultValue.Trim(),
            string.IsNullOrWhiteSpace(unit) ? null : unit.Trim(),
            string.IsNullOrWhiteSpace(referenceRange) ? null : referenceRange.Trim(),
            flag,
            string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim(),
            createdBy);
    }
}
