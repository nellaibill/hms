using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.HR.Infrastructure.Repositories;

internal class EmployeeRepository : IEmployeeRepository
{
    private readonly HRDbContext _dbContext;

    public EmployeeRepository(HRDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Employee employee, CancellationToken cancellationToken)
        => await _dbContext.Employees.AddAsync(employee, cancellationToken);

    public Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Employees.AnyAsync(e => e.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string employeeCode, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.Employees.AnyAsync(e => e.EmployeeCode == employeeCode && e.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedAsync(EmployeeListQuery query, CancellationToken cancellationToken)
    {
        var employees = _dbContext.Employees.AsQueryable();

        if (query.DepartmentId.HasValue)
        {
            employees = employees.Where(e => e.DepartmentId == query.DepartmentId.Value);
        }

        if (query.DesignationId.HasValue)
        {
            employees = employees.Where(e => e.DesignationId == query.DesignationId.Value);
        }

        if (query.EmployeeType.HasValue)
        {
            employees = employees.Where(e => e.EmployeeType == query.EmployeeType.Value);
        }

        if (query.EmploymentStatus.HasValue)
        {
            employees = employees.Where(e => e.EmploymentStatus == query.EmploymentStatus.Value);
        }

        if (query.IsActive.HasValue)
        {
            employees = employees.Where(e => e.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            employees = employees.Where(e =>
                EF.Functions.ILike(e.EmployeeCode, term) ||
                EF.Functions.ILike(e.FirstName, term) ||
                EF.Functions.ILike(e.LastName, term) ||
                EF.Functions.ILike(e.Email, term));
        }

        employees = ApplySort(employees, query.Sort);

        var totalCount = await employees.CountAsync(cancellationToken);
        var items = await employees.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken)
        => _dbContext.Employees.CountAsync(cancellationToken);

    public Task<int> CountActiveAsync(CancellationToken cancellationToken)
        => _dbContext.Employees.CountAsync(e => e.IsActive, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<Employee> ApplySort(IQueryable<Employee> employees, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return employees.OrderBy(e => e.FirstName).ThenBy(e => e.LastName);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "employeecode" => descending ? employees.OrderByDescending(e => e.EmployeeCode) : employees.OrderBy(e => e.EmployeeCode),
            "joiningdate" => descending ? employees.OrderByDescending(e => e.JoiningDate) : employees.OrderBy(e => e.JoiningDate),
            "updatedat" => descending ? employees.OrderByDescending(e => e.UpdatedAt) : employees.OrderBy(e => e.UpdatedAt),
            _ => descending
                ? employees.OrderByDescending(e => e.FirstName).ThenByDescending(e => e.LastName)
                : employees.OrderBy(e => e.FirstName).ThenBy(e => e.LastName),
        };
    }
}
