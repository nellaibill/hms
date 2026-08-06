using HMS.Modules.Documents.Contracts;

namespace HMS.Modules.Documents.Application.Abstractions;

/// <summary>
/// Public: implemented by the module that actually owns a given <see cref="DocumentOwnerType"/>
/// (e.g. HMS.Modules.Patients implements one for <see cref="DocumentOwnerType.Patient"/>) and
/// registered into DI from that module's own <c>Add&lt;Module&gt;Module</c> extension — the
/// same "leaf module depends on this module's public seam" direction already used by
/// HMS.Modules.Products depending on HMS.Modules.Masters (see
/// HMS.Api.Configuration.ModuleRegistration's comment on registration order).
///
/// US-1's acceptance criteria requires uploads to be rejected with 404 when the target
/// record doesn't exist — but only four of ten <see cref="DocumentOwnerType"/> values have a
/// real backend module as of this module's creation (Patient, Staff, Appointment, Billing;
/// see docs/modules/Documents/DocumentManagement.md). DocumentService resolves all registered
/// checkers keyed by <see cref="OwnerType"/>; an owner type with no registered checker is not
/// existence-validated — DocumentService logs a warning and proceeds rather than silently
/// pretending to validate a module that doesn't exist yet.
/// </summary>
public interface IDocumentOwnerExistenceChecker
{
    DocumentOwnerType OwnerType { get; }

    Task<bool> ExistsAsync(Guid ownerId, CancellationToken cancellationToken);
}
