using FluentValidation;
using HMS.Modules.Platform.Contracts;

namespace HMS.Modules.Platform.Application.Validators;

/// <summary>
/// Malformed-request checks only — module keys are not validated against
/// HMS.Shared.Kernel.ModuleCatalog here. An unrecognized key is harmless (it just never
/// matches any Permission.Module, so it's inert — see AuthenticationService.LoginAsync's
/// filter), and this is a Platform-Admin-only, internal-tool endpoint, so the risk of a
/// silent typo is accepted rather than adding a second place the module catalog must stay
/// in sync.
/// </summary>
internal class UpdateTenantConfigurationRequestValidator : AbstractValidator<UpdateTenantConfigurationRequest>
{
    public UpdateTenantConfigurationRequestValidator()
    {
        RuleForEach(x => x.EnabledModules).NotEmpty().WithMessage("Module keys cannot be empty.");
        RuleFor(x => x.SubscriptionTier).NotEmpty().WithMessage("Subscription tier is required.").MaximumLength(50);
    }
}
