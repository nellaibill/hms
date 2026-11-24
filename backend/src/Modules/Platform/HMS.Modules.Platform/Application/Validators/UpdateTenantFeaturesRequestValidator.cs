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

        // Same dependency guard as CreateHospitalRequestValidator — also blocks disabling a
        // dependency (e.g. Products) while its dependent (Pharmacy) stays enabled, since this
        // validates the full desired set on every update, not just newly-added keys.
        RuleFor(x => x.EnabledFeatures)
            .Must(keys => keys.All(key => !FeatureCatalog.Dependencies.TryGetValue(key, out var deps) || deps.All(keys.Contains)))
            .WithMessage(BuildDependencyErrorMessage());

        // Sub-features (ADR-145): known keys only, and each one's needs left on too (e.g.
        // Attendance needs Employees) — the same guard as modules above, on the sub-feature set.
        When(x => x.EnabledSubFeatures is not null, () =>
        {
            RuleForEach(x => x.EnabledSubFeatures)
                .Must(key => FeatureCatalog.SubFeatureKeys.Contains(key))
                .WithMessage("One or more sub-feature keys are not recognized.");

            RuleFor(x => x.EnabledSubFeatures!)
                .Must(keys => keys.All(key => !FeatureCatalog.SubFeatureDependencies.TryGetValue(key, out var deps) || deps.All(keys.Contains)))
                .WithMessage(BuildSubFeatureDependencyErrorMessage());
        });

        static string BuildDependencyErrorMessage()
        {
            var clauses = FeatureCatalog.Dependencies.SelectMany(kv => kv.Value.Select(dep => $"'{kv.Key}' requires '{dep}'"));
            return $"A selected module requires another module that isn't selected ({string.Join(", ", clauses)}).";
        }

        static string BuildSubFeatureDependencyErrorMessage()
        {
            var clauses = FeatureCatalog.SubFeatureDependencies.SelectMany(kv => kv.Value.Select(dep => $"'{kv.Key}' requires '{dep}'"));
            return $"A selected sub-feature requires another that isn't selected ({string.Join(", ", clauses)}).";
        }
    }
}
