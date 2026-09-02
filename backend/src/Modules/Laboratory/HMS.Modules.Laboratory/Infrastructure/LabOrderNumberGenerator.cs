using HMS.Modules.Laboratory.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Laboratory.Infrastructure;

/// <summary>
/// Formats a short, human-readable LabOrderNumber from a real Postgres sequence —
/// coordination-free under concurrent order creation, unlike a MAX(...)+1 query. Mirrors
/// Billing's InvoiceNumberGenerator.
/// </summary>
internal class LabOrderNumberGenerator : ILabOrderNumberGenerator
{
    private readonly LaboratoryDbContext _dbContext;

    public LabOrderNumberGenerator(LaboratoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> NextLabOrderNumberAsync(CancellationToken cancellationToken)
    {
        var fullyQualifiedName = $"{LaboratoryDbContext.SchemaName}.{LaboratoryDbContext.LabOrderNumberSequenceName}";
        var results = await _dbContext.Database
            .SqlQuery<long>($"SELECT nextval({fullyQualifiedName}::regclass)")
            .ToListAsync(cancellationToken);

        return $"LAB-{DateTime.UtcNow:yyyy}-{results[0]:D6}";
    }
}
