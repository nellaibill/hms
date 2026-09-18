using HMS.Modules.OpdConsultation.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.OpdConsultation.Application.Abstractions;

/// <summary>
/// Structures a free-text consultation transcript (dictated or typed) into the note's own
/// narrative fields. Deliberately narrow — no diagnosis/investigation matching, no persistence:
/// the caller (OpdConsultationService) merges the result into the form's own SaveDraft/Complete
/// flow, same as any other field the consultant edits by hand.
///
/// Unlike Notifications' ISmsSender/IEmailSender (best-effort background channels that no-op
/// with a logged warning when unconfigured), a failure here is always surfaced to the caller —
/// the consultant is actively waiting on this call.
/// </summary>
internal interface IClinicalNoteAiClient
{
    Task<Result<StructuredConsultationNoteResponse>> StructureAsync(string transcript, CancellationToken cancellationToken);
}
