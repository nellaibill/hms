namespace HMS.Modules.OpdConsultation.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency inversion
/// rule in docs/Architecture.md — Application never references EF Core types.
/// </summary>
internal interface IOpdConsultationRepository
{
    Task AddAsync(Domain.OpdConsultationNote note, CancellationToken cancellationToken);

    Task<Domain.OpdConsultationNote?> GetByConsultationIdAsync(Guid consultationId, CancellationToken cancellationToken);

    /// <summary>Every note already on file for one patient (read-only, untracked), newest first.</summary>
    Task<IReadOnlyList<Domain.OpdConsultationNote>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken);

    /// <summary>Every note (Draft or Completed) recorded against one visit — backs OPD Billing
    /// Entry's pre-fill of the doctor's catalog investigations (regression report OPD-01).</summary>
    Task<IReadOnlyList<Domain.OpdConsultationNote>> GetByVisitIdAsync(Guid visitId, CancellationToken cancellationToken);

    /// <summary>Completed notes that carry at least one prescription and were last changed
    /// in the given window, newest first — the pharmacy's prescription queue.</summary>
    Task<IReadOnlyList<Domain.OpdConsultationNote>> GetCompletedWithPrescriptionsAsync(DateTime fromUtc, DateTime toUtc, int take, CancellationToken cancellationToken);

    /// <summary>Notes (Draft or Completed) whose review date is on or after <paramref name="from"/>,
    /// soonest first — the OPD page's Scheduled Visits list. Untracked.</summary>
    Task<IReadOnlyList<Domain.OpdConsultationNote>> GetWithReviewDateFromAsync(DateOnly from, int take, CancellationToken cancellationToken);

    /// <summary>Notes (Draft or Completed) whose patient is marked, newest mark first — the OPD
    /// page's Marked Patients list. Untracked.</summary>
    Task<IReadOnlyList<Domain.OpdConsultationNote>> GetMarkedAsync(int take, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
