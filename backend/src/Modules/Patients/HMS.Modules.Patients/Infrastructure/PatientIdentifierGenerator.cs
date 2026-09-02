using HMS.Modules.Patients.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HMS.Modules.Patients.Infrastructure;

/// <summary>
/// Formats the short, human-readable UHID from a real Postgres sequence —
/// coordination-free under concurrent registrations, unlike a MAX(...)+1 query.
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
        // SqlQuery (not SqlQueryRaw) parameterizes the interpolated value rather than
        // concatenating it into the SQL text — kept injection-safe even though the sequence
        // name is, in practice, always this one constant.
        var fullyQualifiedName = $"{PatientsDbContext.SchemaName}.{PatientsDbContext.UhidSequenceName}";
        var results = await _dbContext.Database
            .SqlQuery<long>($"SELECT nextval({fullyQualifiedName}::regclass)")
            .ToListAsync(cancellationToken);

        return $"P-{DateTime.UtcNow:yyyy}-{results[0]:D6}";
    }

    public async Task<string?> NextImportedUhidAsync(CancellationToken cancellationToken)
    {
        var fullyQualifiedName = $"{PatientsDbContext.SchemaName}.{PatientsDbContext.ImportedUhidSequenceName}";
        try
        {
            var results = await _dbContext.Database
                .SqlQuery<long>($"SELECT nextval({fullyQualifiedName}::regclass)")
                .ToListAsync(cancellationToken);

            return $"P-{DateTime.UtcNow:yyyy}-{results[0]:D6}";
        }
        catch (PostgresException ex) when (ex.SqlState == "2200H") // sequence_generator_limit_exceeded
        {
            // "nextval: reached maximum value of sequence" — the sequence's own MAXVALUE
            // (1-40000, see PatientsDbContext) refusing further calls. This is the actual
            // enforcement of the 40,000-record imported-patient cap: the database itself
            // says no, not an application-level count that a bug could skip.
            return null;
        }
    }
}
