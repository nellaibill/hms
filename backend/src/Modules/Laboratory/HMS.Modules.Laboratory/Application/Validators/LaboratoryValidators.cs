using FluentValidation;
using HMS.Modules.Laboratory.Contracts;

namespace HMS.Modules.Laboratory.Application.Validators;

/// <summary>
/// Request-shape validation only (docs/DeveloperHandbook.md §8/§11 — explicit registration in
/// LaboratoryModule.cs, never AddValidatorsFromAssemblyContaining). Exists specifically so the
/// two request shapes whose fields flow into a Domain Guard clause (LabOrderItem.
/// RejectForCorrection's reason, LabResultParameter.Create's ParameterName/ResultValue) never
/// reach the domain empty/whitespace — LabOrderService's per-item mutator catch block only
/// translates InvalidOperationException (an illegal status transition) into a Result.Failure,
/// not ArgumentException, so an unvalidated blank string would otherwise surface as an
/// unhandled 500 instead of a clean 400.
/// </summary>
internal class CollectSampleRequestValidator : AbstractValidator<CollectSampleRequest>
{
    public CollectSampleRequestValidator()
    {
        RuleFor(x => x.SampleType).IsInEnum();
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.Quantity).MaximumLength(50);
        RuleFor(x => x.Remarks).MaximumLength(1000);
    }
}

internal class RejectSampleRequestValidator : AbstractValidator<RejectSampleRequest>
{
    public RejectSampleRequestValidator()
    {
        RuleFor(x => x.Reason).IsInEnum();
        RuleFor(x => x.Remarks).MaximumLength(1000);
    }
}

internal class ResultParameterRequestValidator : AbstractValidator<ResultParameterRequest>
{
    public ResultParameterRequestValidator()
    {
        RuleFor(x => x.ParameterName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ResultValue).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Unit).MaximumLength(50);
        RuleFor(x => x.ReferenceRange).MaximumLength(200);
        RuleFor(x => x.Flag).IsInEnum().When(x => x.Flag.HasValue);
        RuleFor(x => x.Remarks).MaximumLength(1000);
    }
}

internal class SaveResultDraftRequestValidator : AbstractValidator<SaveResultDraftRequest>
{
    public SaveResultDraftRequestValidator()
    {
        RuleForEach(x => x.Parameters).SetValidator(new ResultParameterRequestValidator());
    }
}

internal class RejectForCorrectionRequestValidator : AbstractValidator<RejectForCorrectionRequest>
{
    public RejectForCorrectionRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
