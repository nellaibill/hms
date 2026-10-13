using HMS.Modules.Patients.Contracts;

namespace HMS.Modules.Patients.Application.Abstractions;

/// <summary>
/// Generates the short, human-readable business identifiers — a patient's UHID, a visit's
/// OP No/IP No — distinct from each entity's internal <c>Guid.CreateVersion7()</c> primary
/// key. Implemented in Infrastructure via Postgres sequences.
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

    /// <summary>The number given to a visit as it is recorded: an OP No ("OP-…") for an OP
    /// visit, an IP No ("IP-…") for every other visit type (IP, Emergency, Day Care,
    /// Observation). Each series has its own sequence, so the two never share or skip each
    /// other's numbers.</summary>
    Task<string> NextVisitNumberAsync(VisitType visitType, CancellationToken cancellationToken);
}
