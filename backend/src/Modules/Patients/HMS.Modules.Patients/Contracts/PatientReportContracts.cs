namespace HMS.Modules.Patients.Contracts;

/// <summary>One row of the Patient Reports table — the base PatientResponse plus report-only
/// enrichment (last visit / department) that would otherwise bloat that response for every
/// other caller (registration, edit, enquiry). See PatientRepository.GetLastVisitsAsync for
/// how these two fields are resolved.</summary>
public record PatientReportRowResponse
{
    public PatientResponse Patient { get; init; } = new();
    public DateTime? LastVisitAt { get; init; }
    public Guid? LastVisitDepartmentId { get; init; }
}

/// <summary>Patient Reports' four summary-card numbers — see
/// PatientRepository.GetReportSummaryAsync's own doc comment for exactly what each counts.
/// All four are null-safe zeros (not omitted) when the query's own From/To aren't both
/// supplied, matching PatientListQuery.From's "activity scope" semantics.</summary>
public record PatientReportSummaryResponse
{
    public int TotalPatients { get; init; }
    public int NewPatients { get; init; }
    public int ReturningPatients { get; init; }
    public int TotalVisits { get; init; }
}
