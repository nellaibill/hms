using HMS.Modules.DischargeSummary.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.DischargeSummary.Domain;

/// <summary>
/// The discharge summary aggregate root for one IPD admission. AdmissionId/PatientId are
/// bare Guids validated at the Application layer through IPD's/Patients' public service
/// interfaces — never a real cross-schema DB foreign key (docs/DatabaseArchitecture.md §7,
/// mirrors HMS.Modules.Pharmacy.Domain.PharmacyStockTransaction's identical treatment of
/// PatientId/AdmissionId). Draft → Finalized is the only state transition; once Finalized,
/// the caller (DischargeSummaryService) is responsible for rejecting further updates — this
/// entity itself does not re-check its own Status before applying a mutation, same
/// convention as HMS.Modules.IPD.Domain.Admission.TransferBed/Discharge.
/// </summary>
internal class DischargeSummary : Entity
{
    public Guid AdmissionId { get; private set; }
    public Guid PatientId { get; private set; }
    public DischargeSummaryStatus Status { get; private set; }

    /// <summary>Pre-filled from Admission.FinalDiagnosis at <see cref="Create"/> time by the
    /// caller (DischargeSummaryService) — independently editable from that point via
    /// UpdateClinicalDetails, never re-read from Admission after creation.</summary>
    public string? FinalDiagnosis { get; private set; }

    public Guid? PreparedByUserId { get; private set; }
    public Guid? CheckedByUserId { get; private set; }
    public Guid? ConsultantApprovedByUserId { get; private set; }
    public DateTime? FinalizedAt { get; private set; }
    public Guid? FinalizedByUserId { get; private set; }

    // Required by EF Core materialization.
    private DischargeSummary()
    {
    }

    private DischargeSummary(Guid id, Guid admissionId, Guid patientId, string? finalDiagnosis, Guid? createdBy)
        : base(id, createdBy)
    {
        AdmissionId = admissionId;
        PatientId = patientId;
        FinalDiagnosis = string.IsNullOrWhiteSpace(finalDiagnosis) ? null : finalDiagnosis.Trim();
        Status = DischargeSummaryStatus.Draft;
    }

    /// <summary>Caller (DischargeSummaryService) is responsible for confirming the admission
    /// is Discharged and that no other DischargeSummary already exists for it before calling
    /// this — see docs/DecisionLog.md.</summary>
    public static DischargeSummary Create(Guid admissionId, Guid patientId, string? finalDiagnosisPrefill, Guid? createdBy)
        // Time-ordered UUID per docs/DatabaseArchitecture.md §4.
        => new(Guid.CreateVersion7(), admissionId, patientId, finalDiagnosisPrefill, createdBy);

    /// <summary>Caller (DischargeSummaryService) is responsible for rejecting this call while
    /// already Finalized.</summary>
    public void Finalize(
        Guid? preparedByUserId,
        Guid? checkedByUserId,
        Guid? consultantApprovedByUserId,
        DateTime finalizedAt,
        Guid? finalizedByUserId)
    {
        Status = DischargeSummaryStatus.Finalized;
        PreparedByUserId = preparedByUserId;
        CheckedByUserId = checkedByUserId;
        ConsultantApprovedByUserId = consultantApprovedByUserId;
        FinalizedAt = finalizedAt;
        FinalizedByUserId = finalizedByUserId;
        MarkUpdated(finalizedByUserId);
    }
}
