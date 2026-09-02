namespace HMS.Modules.Patients.Contracts;

/// <summary>Lifecycle of one uploaded import file. Validating/ReadyForReview cover the
/// dry-run pass (no patients/addresses written yet); Committing/Completed cover the pass
/// triggered by the Super Admin's explicit confirmation. Failed means the file itself
/// couldn't be parsed at all (wrong format, corrupt file) — distinct from individual rows
/// being Invalid, which is a normal, expected outcome of a validate pass.</summary>
public enum ImportBatchStatus
{
    Validating,
    ReadyForReview,
    Committing,
    Completed,
    Failed,
}

/// <summary>Per-row outcome. Valid/Invalid are set by the validate pass; Created/CommitFailed
/// are set by the commit pass (only Valid rows are ever committed).</summary>
public enum ImportRowStatus
{
    Valid,
    Invalid,
    Created,
    CommitFailed,
}
