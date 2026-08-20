using FluentValidation;
using HMS.Modules.Platform.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Platform.Application.Validators;

/// <summary>
/// Unlike UpdateTenantConfigurationRequestValidator (which deliberately doesn't validate
/// against ModuleCatalog — see its own doc comment), this DOES validate against
/// FeatureCatalog: an unrecognized feature key here isn't harmless — it drives which
/// database schemas get provisioned/migrated, so a typo must be rejected, not silently
/// ignored. Mandatory features must also always be present in the enabled set — this is the
/// server-side half of "never let a caller disable a mandatory module" (FeatureCatalog's own
/// doc comment); TenantFeatureService additionally forces them on regardless, but rejecting
/// here gives the caller an explicit error instead of a silent override.
/// </summary>
internal class UpdateTenantFeaturesRequestValidator : AbstractValidator<UpdateTenantFeaturesRequest>
{
    public UpdateTenantFeaturesRequestValidator()
    {
        RuleForEach(x => x.EnabledFeatures)
            .Must(key => FeatureCatalog.All.Contains(key))
            .WithMessage("One or more feature keys are not recognized.");

        RuleFor(x => x.EnabledFeatures)
            .Must(keys => FeatureCatalog.Mandatory.All(keys.Contains))
            .WithMessage($"Mandatory features cannot be disabled: {string.Join(", ", FeatureCatalog.Mandatory)}.");
    }
}
