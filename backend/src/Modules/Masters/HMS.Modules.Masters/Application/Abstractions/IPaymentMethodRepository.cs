using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Abstractions;

internal interface IPaymentMethodRepository
{
    Task AddAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken);

    Task<PaymentMethod?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string methodCode, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<PaymentMethod> Items, int TotalCount)> GetPagedAsync(PaymentMethodListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
