using HMS.Modules.Branding.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Branding.Application;

/// <summary>
/// Public (not internal): this is the one Application-layer type BrandingController — which
/// ASP.NET Core requires to be public, with a public constructor, for controller discovery
/// and DI activation — takes as a constructor dependency. A public constructor cannot have
/// an internal parameter type (CS0051), so this interface is the module's deliberate,
/// narrow seam between its public HTTP boundary and its otherwise-internal
/// Application/Domain/Infrastructure layers, mirroring HMS.Modules.Identity.IUserService.
/// </summary>
public interface IBrandingService
{
    /// <summary>Gets the current theme/branding configuration, creating the default row on first call if none exists yet.</summary>
    Task<BrandingResponse> GetAsync(CancellationToken cancellationToken);

    Task<Result<BrandingResponse>> UpdateAsync(UpdateBrandingRequest request, Guid? actorId, CancellationToken cancellationToken);

    /// <param name="slot">A <see cref="BrandingLogoSlots"/> value; anything else fails with BRANDING.INVALID_LOGO_SLOT.</param>
    Task<Result<BrandingResponse>> UploadLogoAsync(string slot, Stream content, string fileName, long length, Guid? actorId, CancellationToken cancellationToken);

    /// <summary>Clears one logo slot (the stored file is left on disk, same as a replaced logo).</summary>
    Task<Result<BrandingResponse>> RemoveLogoAsync(string slot, Guid? actorId, CancellationToken cancellationToken);
}
