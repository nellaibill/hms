using System.Linq.Expressions;
using FluentValidation;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Validators;

internal class CreateDiagnosticServiceRequestValidator : AbstractValidator<CreateDiagnosticServiceRequest>
{
    public CreateDiagnosticServiceRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.ServiceType).IsInEnum();

        // Procedure stays exclusively on the old DiagnosticTest — this normalized entity only
        // ever covers Laboratory/Radiology (see Domain/DiagnosticService.cs).
        RuleFor(x => x.ServiceType)
            .NotEqual(DiagnosticTestServiceType.Procedure)
            .WithMessage("Procedure services must be created as a DiagnosticTest, not a DiagnosticService.");

        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.");
        RuleFor(x => x.CostPrice).GreaterThanOrEqualTo(0).WithMessage("Cost price cannot be negative.");

        RuleFor(x => x.ProviderId)
            .NotNull()
            .When(x => x.IsOutsourced)
            .WithMessage("A provider is required when the service is outsourced.");

        DiagnosticServiceTariffRules.Apply(this, x => x.ServiceType, x => x.DoctorCharge, x => x.BedsidePrice, x => x.BedsideDoctorCharge, x => x.RateOptions);
        ListOrderRules.ApplyListing(this, x => x.ServiceType, x => x.SampleType, x => x.SortOrder);
    }
}

internal class UpdateDiagnosticServiceRequestValidator : AbstractValidator<UpdateDiagnosticServiceRequest>
{
    public UpdateDiagnosticServiceRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.ServiceType).IsInEnum();

        RuleFor(x => x.ServiceType)
            .NotEqual(DiagnosticTestServiceType.Procedure)
            .WithMessage("Procedure services must be created as a DiagnosticTest, not a DiagnosticService.");

        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.");
        RuleFor(x => x.CostPrice).GreaterThanOrEqualTo(0).WithMessage("Cost price cannot be negative.");

        RuleFor(x => x.ProviderId)
            .NotNull()
            .When(x => x.IsOutsourced)
            .WithMessage("A provider is required when the service is outsourced.");

        DiagnosticServiceTariffRules.Apply(this, x => x.ServiceType, x => x.DoctorCharge, x => x.BedsidePrice, x => x.BedsideDoctorCharge, x => x.RateOptions);
        ListOrderRules.ApplyListing(this, x => x.ServiceType, x => x.SampleType, x => x.SortOrder);
    }
}

/// <summary>ADR-154's rules for where an item is listed: a Price List place from 1 up, and a
/// sample type from <see cref="DiagnosticSampleTypes.All"/> on a lab test only.</summary>
internal static class ListOrderRules
{
    public const int Max = 99999;
    public const string Message = "The list order must be a whole number from 1 to 99999.";

    public static void ApplyListing<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, DiagnosticTestServiceType>> serviceType,
        Expression<Func<T, string?>> sampleType,
        Expression<Func<T, int?>> sortOrder)
    {
        var getType = serviceType.Compile();
        var getSortOrder = sortOrder.Compile();

        validator.RuleFor(sortOrder).InclusiveBetween(1, Max).When(x => getSortOrder(x).HasValue).WithMessage(Message);
        validator.RuleFor(sampleType)
            .Must(sample => string.IsNullOrWhiteSpace(sample) || DiagnosticSampleTypes.All.Contains(sample.Trim()))
            .WithMessage($"Sample must be one of: {string.Join(", ", DiagnosticSampleTypes.All)}.");
        validator.RuleFor(sampleType)
            .Must(string.IsNullOrWhiteSpace)
            .When(x => getType(x) != DiagnosticTestServiceType.Laboratory)
            .WithMessage("Only a lab test has a sample.");
    }
}

/// <summary>
/// ADR-140's rules for the Radiology-only tariff parts, shared by the create and update
/// validators: a Laboratory test has no doctor's share, bedside add-on or rate options; the
/// bedside add-on is both parts or neither; rate options have a unique name and no negative
/// amounts.
/// </summary>
internal static class DiagnosticServiceTariffRules
{
    public const int MaxRateOptions = 10;

    public static void Apply<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, DiagnosticTestServiceType>> serviceType,
        Expression<Func<T, decimal>> doctorCharge,
        Expression<Func<T, decimal?>> bedsidePrice,
        Expression<Func<T, decimal?>> bedsideDoctorCharge,
        Expression<Func<T, IEnumerable<DiagnosticServiceRateOptionDto>>> rateOptions)
    {
        var getType = serviceType.Compile();
        var getBedsidePrice = bedsidePrice.Compile();
        var getBedsideDoctorCharge = bedsideDoctorCharge.Compile();
        bool IsLab(T x) => getType(x) == DiagnosticTestServiceType.Laboratory;

        validator.RuleFor(doctorCharge).GreaterThanOrEqualTo(0).WithMessage("Doctor's share cannot be negative.");
        validator.RuleFor(doctorCharge).Equal(0).When(IsLab).WithMessage("A lab test has no doctor's share.");

        validator.RuleFor(bedsidePrice).GreaterThanOrEqualTo(0).When(x => getBedsidePrice(x).HasValue).WithMessage("The bedside add-on cannot be negative.");
        validator.RuleFor(bedsideDoctorCharge).GreaterThanOrEqualTo(0).When(x => getBedsideDoctorCharge(x).HasValue).WithMessage("The bedside add-on cannot be negative.");
        validator.RuleFor(bedsidePrice)
            .Must((x, price) => price.HasValue == getBedsideDoctorCharge(x).HasValue)
            .WithMessage("Set both the bedside price and the bedside doctor's share, or neither.");
        validator.RuleFor(bedsidePrice).Null().When(IsLab).WithMessage("A lab test has no bedside add-on.");

        validator.RuleFor(rateOptions).Must(options => !options.Any()).When(IsLab).WithMessage("A lab test has no rate options.");
        validator.RuleFor(rateOptions)
            .Must(options => options.Count() <= MaxRateOptions)
            .WithMessage($"A service can have at most {MaxRateOptions} rate options.");
        validator.RuleFor(rateOptions)
            .Must(options => options.Select(o => o.Name.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() == options.Count())
            .WithMessage("Each rate option needs a different name.");
        validator.RuleForEach(rateOptions).ChildRules(option =>
        {
            option.RuleFor(o => o.Name).NotEmpty().WithMessage("A rate option needs a name.")
                .MaximumLength(DiagnosticServiceRateOption.NameMaxLength);
            option.RuleFor(o => o.Price).GreaterThanOrEqualTo(0).WithMessage("A rate option's price cannot be negative.");
            option.RuleFor(o => o.DoctorCharge).GreaterThanOrEqualTo(0).WithMessage("A rate option's doctor's share cannot be negative.");
            option.RuleFor(o => o.CostPrice).GreaterThanOrEqualTo(0).WithMessage("A rate option's running cost cannot be negative.");
        });
    }
}
