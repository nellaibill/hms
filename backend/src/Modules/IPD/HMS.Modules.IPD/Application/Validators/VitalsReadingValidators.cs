using FluentValidation;
using HMS.Modules.IPD.Contracts;

namespace HMS.Modules.IPD.Application.Validators;

internal class CreateVitalsReadingRequestValidator : AbstractValidator<CreateVitalsReadingRequest>
{
    public CreateVitalsReadingRequestValidator()
    {
        RuleFor(x => x.RecordedAt).NotEqual(default(DateTime));

        RuleFor(x => x.TemperatureF).InclusiveBetween(70, 115).When(x => x.TemperatureF.HasValue);
        RuleFor(x => x.PulseRate).InclusiveBetween(0, 300).When(x => x.PulseRate.HasValue);
        RuleFor(x => x.RespiratoryRate).InclusiveBetween(0, 150).When(x => x.RespiratoryRate.HasValue);
        RuleFor(x => x.BloodPressureSystolic).InclusiveBetween(40, 300).When(x => x.BloodPressureSystolic.HasValue);
        RuleFor(x => x.BloodPressureDiastolic).InclusiveBetween(20, 200).When(x => x.BloodPressureDiastolic.HasValue);
        RuleFor(x => x.SpO2Percent).InclusiveBetween(0, 100).When(x => x.SpO2Percent.HasValue);
        RuleFor(x => x.WeightKg).InclusiveBetween(0, 500).When(x => x.WeightKg.HasValue);
        RuleFor(x => x.HeightCm).InclusiveBetween(0, 300).When(x => x.HeightCm.HasValue);
        RuleFor(x => x.PainScore).InclusiveBetween(0, 10).When(x => x.PainScore.HasValue);
        RuleFor(x => x.BloodGlucoseMgDl).InclusiveBetween(0, 1000).When(x => x.BloodGlucoseMgDl.HasValue);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
