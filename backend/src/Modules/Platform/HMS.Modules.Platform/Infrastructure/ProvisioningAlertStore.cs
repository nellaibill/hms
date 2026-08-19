using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Platform.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Platform.Infrastructure;

internal sealed class ProvisioningAlertStore : IProvisioningAlertStore
{
    private readonly PlatformDbContext _dbContext;

    public ProvisioningAlertStore(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RaiseAsync(string databaseName, string message, CancellationToken cancellationToken)
    {
        await _dbContext.ProvisioningAlerts.AddAsync(ProvisioningAlert.Raise(databaseName, message), cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<int> GetCountAsync(CancellationToken cancellationToken) =>
        _dbContext.ProvisioningAlerts.CountAsync(cancellationToken);
}
