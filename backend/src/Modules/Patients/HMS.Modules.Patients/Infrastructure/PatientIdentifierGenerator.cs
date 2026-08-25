using HMS.Modules.Patients.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

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
}
