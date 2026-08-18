using HMS.Modules.Billing.Application.Abstractions;
using HMS.Modules.Billing.Domain;

namespace HMS.Modules.Billing.Infrastructure.Repositories;

internal class PaymentRepository : IPaymentRepository
{
    private readonly BillingDbContext _dbContext;

    public PaymentRepository(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken)
        => await _dbContext.Payments.AddAsync(payment, cancellationToken);
}
