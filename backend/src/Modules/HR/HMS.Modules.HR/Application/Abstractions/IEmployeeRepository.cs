using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;

namespace HMS.Modules.HR.Application.Abstractions;

internal interface IEmployeeRepository
{
    Task AddAsync(Employee employee, CancellationToken cancellationToken);

    Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string employeeCode, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedAsync(EmployeeListQuery query, CancellationToken cancellationToken);

    Task<int> CountAsync(CancellationToken cancellationToken);

    Task<int> CountActiveAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
