using FluentValidation;
using HMS.Modules.Documents.Contracts;

namespace HMS.Modules.Documents.Application.Validators;

/// <summary>Validates the form-field portion of an upload; the file itself is validated
/// directly in DocumentService.UploadAsync (size/content-type/signature — see that class's
/// remarks for why FluentValidation isn't a natural fit for stream-shaped input).</summary>
internal class UploadDocumentRequestValidator : AbstractValidator<UploadDocumentRequest>
{
    public UploadDocumentRequestValidator()
    {
        RuleFor(x => x.OwnerType).IsInEnum();
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.DocumentType).IsInEnum();
        RuleFor(x => x.Classification).IsInEnum().When(x => x.Classification.HasValue);
    }
}
