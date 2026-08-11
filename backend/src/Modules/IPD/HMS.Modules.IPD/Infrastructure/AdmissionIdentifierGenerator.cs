using HMS.Modules.IPD.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.IPD.Infrastructure;

/// <summary>
/// Formats a short, human-readable AdmissionNumber from a real Postgres sequence —
/// coordination-free under concurrent admissions, unlike a MAX(...)+1 query. Mirrors
/// Patients' PatientIdentifierGenerator.
/// </summary>
internal class AdmissionIdentifierGenerator : IAdmissionIdentifierGenerator
{
    private readonly IPDDbContext _dbContext;

    public AdmissionIdentifierGenerator(IPDDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> NextAdmissionNumberAsync(CancellationToken cancellationToken)
    {
        var fullyQualifiedName = $"{IPDDbContext.SchemaName}.{IPDDbContext.AdmissionNumberSequenceName}";
        var results = await _dbContext.Database
            .SqlQuery<long>($"SELECT nextval({fullyQualifiedName}::regclass)")
            .ToListAsync(cancellationToken);

        return $"ADM-{DateTime.UtcNow:yyyy}-{results[0]:D6}";
    }
}
