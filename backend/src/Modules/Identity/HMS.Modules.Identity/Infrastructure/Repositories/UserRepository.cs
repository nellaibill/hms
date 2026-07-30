using HMS.Modules.Identity.Application.Abstractions;
using HMS.Modules.Identity.Contracts;
using HMS.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Identity.Infrastructure.Repositories;

internal class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _dbContext;

    public UserRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
        => await _dbContext.Users.AddAsync(user, cancellationToken);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _dbContext.Users.FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);
    }

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        var normalized = username.Trim().ToLowerInvariant();
        return _dbContext.Users.FirstOrDefaultAsync(u => u.Username == normalized, cancellationToken);
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(UserListQuery query, CancellationToken cancellationToken)
    {
        var users = _dbContext.Users.AsQueryable();

        if (query.IsActive.HasValue)
        {
            users = users.Where(u => u.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            users = users.Where(u =>
                EF.Functions.ILike(u.Username, term) ||
                EF.Functions.ILike(u.FirstName, term) ||
                EF.Functions.ILike(u.LastName, term) ||
                EF.Functions.ILike(u.Email, term));
        }

        users = ApplySort(users, query.Sort);

        var totalCount = await users.CountAsync(cancellationToken);

        var items = await users
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<User> ApplySort(IQueryable<User> users, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return users.OrderByDescending(u => u.CreatedAt);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "username" => descending ? users.OrderByDescending(u => u.Username) : users.OrderBy(u => u.Username),
            "firstname" => descending ? users.OrderByDescending(u => u.FirstName) : users.OrderBy(u => u.FirstName),
            "lastname" => descending ? users.OrderByDescending(u => u.LastName) : users.OrderBy(u => u.LastName),
            "email" => descending ? users.OrderByDescending(u => u.Email) : users.OrderBy(u => u.Email),
            "createdat" => descending ? users.OrderByDescending(u => u.CreatedAt) : users.OrderBy(u => u.CreatedAt),
            _ => users.OrderByDescending(u => u.CreatedAt),
        };
    }
}
