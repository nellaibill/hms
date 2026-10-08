namespace HMS.Shared.Kernel;

/// <summary>
/// The one place that knows where a hospital's files live on disk (see docs/DecisionLog.md
/// ADR-087). Everything a tenant uploads sits under a single "Tenant/{tenantId}" folder, one
/// subfolder per kind:
/// <code>
/// wwwroot/uploads/Tenant/{tenantId}/branding|users|consultants|products/…   (served by UseStaticFiles)
/// App_Data/Tenant/{tenantId}/documents/…                                    (private, never served directly)
/// </code>
/// Every file-storing module, the files zip (HMS.Modules.Backups' TenantFilesArchive) and
/// HMS.Api's TenantFileStorageMigrator build their paths from here, so the layout can't drift
/// between them. Plain path arithmetic only (no IHostEnvironment) so it stays in Kernel.
/// </summary>
public static class TenantFileLocations
{
    public const string TenantFolderName = "Tenant";

    public const string Branding = "branding";
    public const string Users = "users";
    public const string Consultants = "consultants";
    public const string Products = "products";
    public const string Documents = "documents";

    /// <summary>"wwwroot/uploads/Tenant/{tenantId}/{kind}" — publicly served uploads.</summary>
    public static string PublicDirectory(string contentRootPath, Guid tenantId, string kind)
        => Path.Combine(contentRootPath, "wwwroot", "uploads", TenantFolderName, tenantId.ToString(), kind);

    /// <summary>"App_Data/Tenant/{tenantId}/{kind}" — outside wwwroot, so UseStaticFiles can
    /// never serve it.</summary>
    public static string PrivateDirectory(string contentRootPath, Guid tenantId, string kind)
        => Path.Combine(contentRootPath, "App_Data", TenantFolderName, tenantId.ToString(), kind);

    /// <summary>The server-relative path stored in the owning entity's column and served as a
    /// URL: "uploads/Tenant/{tenantId}/{kind}/{segments…}".</summary>
    public static string PublicRelativePath(Guid tenantId, string kind, params string[] segments)
        => string.Join('/', new[] { "uploads", TenantFolderName, tenantId.ToString(), kind }.Concat(segments));

    /// <summary>Maps a stored relative path ("uploads/…") onto its file under wwwroot.</summary>
    public static string PublicFullPath(string contentRootPath, string relativePath)
        => Path.Combine(contentRootPath, "wwwroot", relativePath.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>
    /// True only for a path inside <paramref name="tenantId"/>'s own <paramref name="kind"/>
    /// folder, with no "." / ".." segments. The gate every delete goes through: a path in an
    /// older layout, or another tenant's folder, is never deleted by request-path code.
    /// </summary>
    public static bool IsInTenantPublicFolder(string? relativePath, Guid tenantId, string kind)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || relativePath.Contains('\\'))
        {
            return false;
        }

        var prefix = PublicRelativePath(tenantId, kind) + "/";
        if (!relativePath.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var rest = relativePath[prefix.Length..].Split('/');
        return rest.All(segment => segment.Length > 0 && segment != "." && segment != "..");
    }

    /// <summary>
    /// Maps a stored public path in any layout this app has used onto the current one, for
    /// HMS.Api's migrate-tenant-files command:
    /// <code>
    /// uploads/{kind}/{tail…}                       original, shared by every tenant
    /// uploads/{kind}/{tenantId}/{tail…}            ADR-083
    /// uploads/Tenant/{tenantId}/{kind}/{tail…}     ADR-087 (current — returned unchanged)
    /// </code>
    /// Returns null for any other shape (including another tenant's folder), which the caller
    /// leaves untouched. <paramref name="renameFolder"/> renames the first tail segment
    /// (Branding's primary slot moved from "logo/" to "primary/"). Delete together with the
    /// migrator once every host has been migrated.
    /// </summary>
    public static string? ToCurrentPublicPath(string relativePath, Guid tenantId, string kind, (string From, string To)? renameFolder = null)
    {
        var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 3 || segments[0] != "uploads" || segments.Any(segment => segment is "." or ".." || segment.Contains('\\')))
        {
            return null;
        }

        var tenant = tenantId.ToString();
        string[] tail;
        if (segments[1] == TenantFolderName)
        {
            if (segments.Length < 5 || segments[2] != tenant || segments[3] != kind)
            {
                return null;
            }

            tail = segments[4..];
        }
        else if (segments[1] == kind)
        {
            tail = segments[2] == tenant ? segments[3..] : segments[2..];
        }
        else
        {
            return null;
        }

        if (tail.Length == 0)
        {
            return null;
        }

        if (renameFolder is { } rename && tail.Length > 1 && tail[0] == rename.From)
        {
            tail = [rename.To, .. tail[1..]];
        }

        return PublicRelativePath(tenantId, kind, tail);
    }
}
