using HMS.Modules.Documents.Contracts;

namespace HMS.Modules.Documents.Application.Security;

/// <summary>
/// US-2's access check. There is no ASP.NET Core policy-based authorization infrastructure
/// anywhere in this codebase yet — <c>AddAuthorization()</c> is called with defaults and the
/// only existing <c>[Authorize]</c> usage (AuthenticationController.Me()) is a bare
/// authentication-only gate, per docs/modules/Documents/DocumentManagement.md's backend
/// survey. Standing up the full dynamic Permission/RolePermission-driven policy provider
/// described in docs/ApiStandards.md §9 is a platform-wide undertaking, not something this
/// module should do unilaterally. This class is the pragmatic, explicitly-scoped middle
/// ground: a real, enforced, per-owner-type-and-classification check today, expressed as an
/// in-code table keyed by the stable LoginType claim (see DocumentActor) — swappable for a
/// database-backed policy once Authorization.md's dynamic model is wired in for every module,
/// not just this one.
///
/// superAdmin/admin bypass every check, mirroring
/// frontend/web/src/config/navigation.ts's filterNavigationForRole treatment of those two
/// roles as seeing everything regardless of a leaf's own roles list.
/// </summary>
internal class DocumentAccessPolicy : IDocumentAccessPolicy
{
    private static readonly HashSet<string> BypassRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "superAdmin",
        "admin",
    };

    /// <summary>Which owner types each LoginType may read/write documents for. Deliberately
    /// coarser than a full classification matrix — see class remarks.</summary>
    private static readonly Dictionary<string, DocumentOwnerType[]> OwnerTypesByRole =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["receptionist"] = [DocumentOwnerType.Patient, DocumentOwnerType.Appointment, DocumentOwnerType.Billing],
            ["doctor"] = [DocumentOwnerType.Patient, DocumentOwnerType.Appointment, DocumentOwnerType.Admission, DocumentOwnerType.Lab, DocumentOwnerType.Radiology, DocumentOwnerType.Doctor],
            ["nurse"] = [DocumentOwnerType.Patient, DocumentOwnerType.Admission],
            ["labTechnician"] = [DocumentOwnerType.Lab, DocumentOwnerType.Radiology],
            ["radiologist"] = [DocumentOwnerType.Radiology, DocumentOwnerType.Lab],
            ["pharmacist"] = [DocumentOwnerType.Patient],
            ["hr"] = [DocumentOwnerType.Staff, DocumentOwnerType.Doctor],
            ["accounts"] = [DocumentOwnerType.Billing, DocumentOwnerType.Vendor, DocumentOwnerType.Asset],
        };

    /// <summary>Roles allowed to read/write Restricted-classification documents, regardless
    /// of their normal owner-type access above — the one piece of classification-driven
    /// gating this iteration implements (see Domain.DocumentClassification).</summary>
    private static readonly HashSet<string> RestrictedAccessRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "doctor",
        "nurse",
    };

    public bool CanRead(DocumentActor actor, DocumentOwnerType ownerType, DocumentClassification classification)
    {
        if (!HasOwnerTypeAccess(actor, ownerType))
        {
            return false;
        }

        if (classification == DocumentClassification.Restricted && !IsBypass(actor) && !RestrictedAccessRoles.Contains(actor.LoginType ?? string.Empty))
        {
            return false;
        }

        return true;
    }

    public bool CanWrite(DocumentActor actor, DocumentOwnerType ownerType) => HasOwnerTypeAccess(actor, ownerType);

    private static bool HasOwnerTypeAccess(DocumentActor actor, DocumentOwnerType ownerType)
    {
        if (IsBypass(actor))
        {
            return true;
        }

        return actor.LoginType is not null
            && OwnerTypesByRole.TryGetValue(actor.LoginType, out var allowed)
            && allowed.Contains(ownerType);
    }

    private static bool IsBypass(DocumentActor actor) => actor.LoginType is not null && BypassRoles.Contains(actor.LoginType);
}
