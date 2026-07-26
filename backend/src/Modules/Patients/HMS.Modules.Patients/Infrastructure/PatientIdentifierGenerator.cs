using HMS.Modules.Patients.Application.Abstractions;
using HMS.Modules.Patients.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Patients.Infrastructure;

/// <summary>
/// Formats short, human-readable business identifiers (UHID, registration number) from
/// real Postgres sequences — coordination-free under concurrent registrations, unlike a
/// MAX(...)+1 query. See docs/PatientRegistrationModule.md §18 ("UHID, large, copyable").
/// </summary>
internal class PatientIdentifierGenerator : IPatientIdentifierGenerator
{
    private readonly PatientsDbContext _dbContext;

    public PatientIdentifierGenerator(PatientsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> NextUhidAsync(CancellationToken cancellationToken)
    {
        var sequenceValue = await NextSequenceValueAsync(PatientsDbContext.UhidSequenceName, cancellationToken);
        return $"P-{DateTime.UtcNow:yyyy}-{sequenceValue:D6}";
    }

    public async Task<string> NextRegistrationNumberAsync(EncounterType encounterType, CancellationToken cancellationToken)
    {
        var sequenceValue = await NextSequenceValueAsync(PatientsDbContext.RegistrationNumberSequenceName, cancellationToken);
        var prefix = encounterType switch
        {
            EncounterType.OP => "OP",
            EncounterType.IP => "IP",
            EncounterType.Emergency => "ER",
            EncounterType.DayCare => "DC",
            _ => "OP",
        };

        return $"{prefix}-{DateTime.UtcNow:yyyy}-{sequenceValue:D6}";
    }

    private async Task<long> NextSequenceValueAsync(string sequenceName, CancellationToken cancellationToken)
    {
        // SqlQuery (not SqlQueryRaw) parameterizes the interpolated value rather than
        // concatenating it into the SQL text — Postgres's nextval(regclass) accepts a
        // parameterized text value cast to regclass, so this stays injection-safe even
        // though sequenceName is, in practice, always one of this class's own constants.
        var fullyQualifiedName = $"{PatientsDbContext.SchemaName}.{sequenceName}";
        var results = await _dbContext.Database
            .SqlQuery<long>($"SELECT nextval({fullyQualifiedName}::regclass)")
            .ToListAsync(cancellationToken);

        return results[0];
    }
}
