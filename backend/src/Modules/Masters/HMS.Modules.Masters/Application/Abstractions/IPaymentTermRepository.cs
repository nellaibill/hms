using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Abstractions;

internal interface IPaymentTermRepository
{
    Task AddAsync(PaymentTerm paymentTerm, CancellationToken cancellationToken);

    Task<PaymentTerm?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(string termName, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<PaymentTerm> Items, int TotalCount)> GetPagedAsync(PaymentTermListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
