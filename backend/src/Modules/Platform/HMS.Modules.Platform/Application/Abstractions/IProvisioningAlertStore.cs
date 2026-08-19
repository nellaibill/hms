namespace HMS.Modules.Platform.Application.Abstractions;

/// <summary>
/// Public seam so HMS.Api's TenantProvisioningService (which implements
/// <see cref="ITenantProvisioner"/>) can raise a durable, dashboard-visible alert when a
/// rollback fails, instead of only logging it. See ProvisioningAlert's own doc comment.
/// </summary>
public interface IProvisioningAlertStore
{
    Task RaiseAsync(string databaseName, string message, CancellationToken cancellationToken);

    Task<int> GetCountAsync(CancellationToken cancellationToken);
}
