using FluentValidation;
using HMS.Modules.IPD.Contracts;

namespace HMS.Modules.IPD.Application.Validators;

internal class CreateNursingNoteRequestValidator : AbstractValidator<CreateNursingNoteRequest>
{
    public CreateNursingNoteRequestValidator()
    {
        RuleFor(x => x.NoteDateTime).NotEqual(default(DateTime));
        RuleFor(x => x.Shift).IsInEnum();

        RuleFor(x => x.Observation).MaximumLength(2000);
        RuleFor(x => x.Intervention).MaximumLength(2000);
        RuleFor(x => x.PatientResponse).MaximumLength(2000);
        RuleFor(x => x.Remarks).MaximumLength(2000);
    }
}
