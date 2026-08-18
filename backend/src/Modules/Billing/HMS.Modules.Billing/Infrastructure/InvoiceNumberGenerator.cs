using HMS.Modules.Billing.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Billing.Infrastructure;

/// <summary>
/// Formats a short, human-readable InvoiceNumber from a real Postgres sequence —
/// coordination-free under concurrent invoice creation, unlike a MAX(...)+1 query. Mirrors
/// IPD's AdmissionIdentifierGenerator.
/// </summary>
internal class InvoiceNumberGenerator : IInvoiceNumberGenerator
{
    private readonly BillingDbContext _dbContext;

    public InvoiceNumberGenerator(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> NextInvoiceNumberAsync(CancellationToken cancellationToken)
    {
        var fullyQualifiedName = $"{BillingDbContext.SchemaName}.{BillingDbContext.InvoiceNumberSequenceName}";
        var results = await _dbContext.Database
            .SqlQuery<long>($"SELECT nextval({fullyQualifiedName}::regclass)")
            .ToListAsync(cancellationToken);

        return $"INV-{DateTime.UtcNow:yyyy}-{results[0]:D6}";
    }
}
