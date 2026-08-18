using HMS.Modules.Billing.Domain;

namespace HMS.Modules.Billing.Application.Abstractions;

internal interface IPaymentRepository
{
    Task AddAsync(Payment payment, CancellationToken cancellationToken);
}
