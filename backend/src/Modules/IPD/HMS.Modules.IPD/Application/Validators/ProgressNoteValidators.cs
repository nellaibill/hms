using FluentValidation;
using HMS.Modules.IPD.Contracts;

namespace HMS.Modules.IPD.Application.Validators;

internal class CreateProgressNoteRequestValidator : AbstractValidator<CreateProgressNoteRequest>
{
    public CreateProgressNoteRequestValidator()
    {
        RuleFor(x => x.NoteDateTime).NotEqual(default(DateTime));

        RuleFor(x => x.ClinicalCondition).MaximumLength(2000);
        RuleFor(x => x.Progress).MaximumLength(2000);
        RuleFor(x => x.Diagnosis).MaximumLength(2000);
        RuleFor(x => x.Assessment).MaximumLength(2000);
        RuleFor(x => x.Plan).MaximumLength(2000);
        RuleFor(x => x.Instructions).MaximumLength(2000);
    }
}
