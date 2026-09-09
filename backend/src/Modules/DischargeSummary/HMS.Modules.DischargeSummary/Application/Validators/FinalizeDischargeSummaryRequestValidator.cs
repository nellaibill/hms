using FluentValidation;
using HMS.Modules.DischargeSummary.Contracts;

namespace HMS.Modules.DischargeSummary.Application.Validators;

/// <summary>
/// Every field on FinalizeDischargeSummaryRequest is optional (docs/DecisionLog.md's
/// sign-off decision), so there are no RuleFor entries today — kept as a real validator
/// class (rather than omitted) so DischargeSummariesController.Finalize follows the same
/// validate-then-call-service shape as every other action, and so a future required field
/// has an obvious place to add its rule.
/// </summary>
internal class FinalizeDischargeSummaryRequestValidator : AbstractValidator<FinalizeDischargeSummaryRequest>
{
}
