using FluentValidation;
using HMS.Modules.IPD.Contracts;

namespace HMS.Modules.IPD.Application.Validators;

internal class CreateNursingAssessmentRequestValidator : AbstractValidator<CreateNursingAssessmentRequest>
{
    public CreateNursingAssessmentRequestValidator()
    {
        RuleFor(x => x.AssessedAt).NotEqual(default(DateTime));

        RuleFor(x => x.GeneralCondition).MaximumLength(500);
        RuleFor(x => x.ConsciousnessLevel).MaximumLength(200);
        RuleFor(x => x.Mobility).MaximumLength(500);
        RuleFor(x => x.NutritionStatus).MaximumLength(500);
        RuleFor(x => x.FallRisk).MaximumLength(200);
        RuleFor(x => x.PressureSoreRisk).MaximumLength(200);
        RuleFor(x => x.SkinCondition).MaximumLength(500);
        RuleFor(x => x.PainScore).InclusiveBetween(0, 10).When(x => x.PainScore.HasValue);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
