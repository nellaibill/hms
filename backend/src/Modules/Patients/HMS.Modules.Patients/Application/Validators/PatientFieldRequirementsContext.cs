using FluentValidation;
using HMS.Modules.Patients.Contracts;

namespace HMS.Modules.Patients.Application.Validators;

/// <summary>
/// Carries the hospital's <see cref="PatientFieldRequirements"/> (ADR-135) through a
/// validation run in <see cref="IValidationContext.RootContextData"/>, which FluentValidation
/// shares with child validators (AddressRequestValidator, EmergencyContactRequestValidator) —
/// so the rules stay synchronous, and a validator run without any setting loaded (a unit
/// test calling Validate) keeps the pre-ADR-135 rules (PatientFieldRequirements.BulkImport).
/// </summary>
internal static class PatientFieldRequirementsContext
{
    private const string Key = "HMS.Patients.FieldRequirements";

    public static PatientFieldRequirements Get(IValidationContext context)
        => context.RootContextData.TryGetValue(Key, out var value) && value is PatientFieldRequirements requirements
            ? requirements
            : PatientFieldRequirements.BulkImport;

    public static bool IsSet(IValidationContext context) => context.RootContextData.ContainsKey(Key);

    public static void Set(IValidationContext context, PatientFieldRequirements requirements)
        => context.RootContextData[Key] = requirements;

    /// <summary>For a caller that must not follow the hospital's settings (the bulk import):
    /// a context pinned to <see cref="PatientFieldRequirements.BulkImport"/>.</summary>
    public static ValidationContext<T> ForBulkImport<T>(T instance)
    {
        var context = new ValidationContext<T>(instance);
        Set(context, PatientFieldRequirements.BulkImport);
        return context;
    }
}

/// <summary>Loads the hospital's settings into the context before an async validation run,
/// unless the caller already put some there.</summary>
internal abstract class PatientFieldAwareValidator<T> : AbstractValidator<T>
{
    private readonly IPatientFieldSettingsService? _settings;

    protected PatientFieldAwareValidator(IPatientFieldSettingsService? settings)
    {
        _settings = settings;
    }

    protected static PatientFieldRequirements Required(IValidationContext context) => PatientFieldRequirementsContext.Get(context);

    public override async Task<FluentValidation.Results.ValidationResult> ValidateAsync(ValidationContext<T> context, CancellationToken cancellation = default)
    {
        if (_settings is not null && !PatientFieldRequirementsContext.IsSet(context))
        {
            PatientFieldRequirementsContext.Set(context, await _settings.GetAsync(cancellation));
        }

        return await base.ValidateAsync(context, cancellation);
    }
}

internal class UpdatePatientFieldSettingsRequestValidator : AbstractValidator<PatientFieldRequirements>
{
    public UpdatePatientFieldSettingsRequestValidator()
    {
        RuleFor(x => x.State)
            .Equal(true).WithMessage("State must be mandatory while District is — a district is picked from the chosen state.")
            .When(x => x.District);
    }
}
