namespace HMS.Modules.Patients.Application.Abstractions;

/// <summary>
/// Generates the short, human-readable UHID business identifier — distinct from the
/// entity's internal <c>Guid.CreateVersion7()</c> primary key. Implemented in Infrastructure
/// via a Postgres sequence.
/// </summary>
internal interface IPatientIdentifierGenerator
{
    Task<string> NextUhidAsync(CancellationToken cancellationToken);

    /// <summary>Draws from the dedicated 1-40000 range reserved for bulk-imported patients
    /// (PatientsDbContext.ImportedUhidSequenceName) — never overlaps with NextUhidAsync's
    /// 40001+ range. Returns null once that range is exhausted: the database itself refuses
    /// (the sequence's own MAXVALUE), not an application-level count, so the 40,000 cap holds
    /// even against a bug or a direct SQL insert bypassing this class entirely.</summary>
    Task<string?> NextImportedUhidAsync(CancellationToken cancellationToken);
}
