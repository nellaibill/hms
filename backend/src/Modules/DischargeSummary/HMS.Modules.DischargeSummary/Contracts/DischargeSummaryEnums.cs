namespace HMS.Modules.DischargeSummary.Contracts;

/// <summary>
/// Two states only, per the approved plan — the richer Prepared/Checked/Approved sign-off
/// pipeline is captured as a set of optional Guid fields stamped once at Finalize, not as a
/// multi-step workflow with its own states (see DischargeSummaryResponse's sign-off fields).
/// </summary>
public enum DischargeSummaryStatus
{
    Draft,
    Finalized,
}
