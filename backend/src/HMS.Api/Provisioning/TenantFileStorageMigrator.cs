using HMS.Modules.Platform.Application.Abstractions;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace HMS.Api.Provisioning;

/// <summary>
/// Explicitly-invoked (never automatic) fixup that moves every tenant's files into its own
/// "Tenant/{tenantId}" folder (see <see cref="TenantFileLocations"/> and docs/DecisionLog.md
/// ADR-087). Files may still be in either of two older layouts: the original one shared by
/// every tenant ("uploads/{kind}/…", "App_Data/documents/…") or ADR-083's kind-first one
/// ("uploads/{kind}/{tenantId}/…", "App_Data/documents/{tenantId}/…"). Both are moved
/// straight to the current layout, and the stored path column is rewritten for every kind
/// that stores a full path — everything except Documents, whose storage_key is a bare file
/// name.
///
/// Idempotent and safe to re-run: anything already in the current layout is left alone, so a
/// partially-completed or repeated run just picks up where it left off. One tenant/one row
/// failing is logged and skipped rather than aborting the whole run — same "don't let one bad
/// row take down the batch" posture as DailyBackupSchedulerService.
///
/// Invoked via `dotnet HMS.Api.dll migrate-tenant-files [--dry-run]` (see Program.cs) —
/// deliberately a separate, explicitly-typed command from the schema `migrate` step, and
/// deliberately not folded into Development's auto-migrate-on-startup, because unlike a
/// schema migration this one moves real files and rewrites real rows; an operator should
/// run `--dry-run` first, read the counts, and only then run it for real.
/// </summary>
public sealed class TenantFileStorageMigrator
{
    // (slot folder, column) for each Branding logo slot. The primary slot's folder was "logo"
    // before ADR-087, so its files are renamed to "primary" on the way.
    private static readonly (string Slot, string Column)[] BrandingLogoColumns =
    [
        ("primary", "logo_path"),
        ("compact", "compact_logo_path"),
        ("login", "login_logo_path"),
        ("print", "print_logo_path"),
        ("favicon", "favicon_path"),
    ];

    private readonly string _contentRootPath;
    private readonly ILogger<TenantFileStorageMigrator> _logger;

    public TenantFileStorageMigrator(IHostEnvironment environment, ILogger<TenantFileStorageMigrator> logger)
    {
        _contentRootPath = environment.ContentRootPath;
        _logger = logger;
    }

    public async Task RunAsync(IReadOnlyList<TenantInfo> tenants, bool dryRun, CancellationToken cancellationToken)
    {
        foreach (var tenant in tenants)
        {
            _logger.LogInformation(
                "--- Tenant '{HospitalCode}' ({TenantId}){DryRunSuffix} ---",
                tenant.HospitalCode, tenant.Id, dryRun ? " [DRY RUN]" : "");

            try
            {
                await using var connection = new NpgsqlConnection(tenant.ConnectionString);
                await connection.OpenAsync(cancellationToken);

                // Each kind runs in its own try/catch (not one try/catch around the whole
                // tenant): Tenant Feature/Module Management means a given tenant may not have
                // every module enabled/migrated (e.g. no "products" schema at all), which
                // surfaces here as a 42P01 "relation does not exist" the moment that kind's
                // query runs — that must skip only this one kind, not abandon the others
                // (found live: a tenant missing products.product_images otherwise also lost
                // its Branding logo check, which has nothing to do with Products).
                await RunKindAsync(tenant, "Documents", () => MigrateDocumentsAsync(connection, tenant, dryRun, cancellationToken));
                await RunKindAsync(tenant, "Consultant photos", () => MigratePathColumnAsync(
                    connection, tenant, dryRun, cancellationToken,
                    label: "Consultant photos", kind: TenantFileLocations.Consultants, table: "masters.consultants", column: "photo_url"));
                await RunKindAsync(tenant, "User photos", () => MigratePathColumnAsync(
                    connection, tenant, dryRun, cancellationToken,
                    label: "User photos", kind: TenantFileLocations.Users, table: "identity.users", column: "profile_photo_url"));
                await RunKindAsync(tenant, "Product images", () => MigratePathColumnAsync(
                    connection, tenant, dryRun, cancellationToken,
                    label: "Product images", kind: TenantFileLocations.Products, table: "products.product_images", column: "image_url"));

                foreach (var (slot, column) in BrandingLogoColumns)
                {
                    var label = $"Branding logo ({slot})";
                    await RunKindAsync(tenant, label, () => MigratePathColumnAsync(
                        connection, tenant, dryRun, cancellationToken,
                        label, TenantFileLocations.Branding, table: "branding.branding_settings", column,
                        renameFolder: slot == "primary" ? ("logo", "primary") : null));
                }

                if (!dryRun)
                {
                    RemoveEmptyPreviousFolders(tenant);
                }
            }
            catch (Exception ex)
            {
                // Only reachable for a failure that isn't specific to one kind (e.g. the
                // connection itself couldn't open) — same "don't let one tenant stop the rest"
                // reasoning as DailyBackupSchedulerService's per-tenant try/catch.
                _logger.LogError(ex, "Failed to migrate tenant '{HospitalCode}' ({TenantId}) — skipping it; re-run this command to retry.", tenant.HospitalCode, tenant.Id);
            }
        }
    }

    /// <summary>
    /// Runs one file kind's migration for one tenant, isolating its failure from the other
    /// kinds — see the "Each kind runs in its own try/catch" note in RunAsync above for why
    /// this matters (a tenant missing one module's schema entirely is a real, expected case,
    /// not a bug).
    /// </summary>
    private async Task RunKindAsync(TenantInfo tenant, string label, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            _logger.LogInformation(
                "  {Label}: skipped for tenant '{HospitalCode}' — its database has no schema for this module (not enabled/migrated for this tenant).",
                label, tenant.HospitalCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "  {Label}: failed for tenant '{HospitalCode}' ({TenantId}) — skipped; re-run this command to retry.", label, tenant.HospitalCode, tenant.Id);
        }
    }

    /// <summary>
    /// Documents: the stored `storage_key` column is only ever a bare filename
    /// ("{documentId}{ext}") — DocumentFileStorage resolves the tenant folder itself from
    /// ITenantContext at read/write time, so no DB rewrite is needed, only the physical move.
    /// </summary>
    private async Task MigrateDocumentsAsync(NpgsqlConnection connection, TenantInfo tenant, bool dryRun, CancellationToken cancellationToken)
    {
        var targetFolder = TenantFileLocations.PrivateDirectory(_contentRootPath, tenant.Id, TenantFileLocations.Documents);
        string[] previousFolders =
        [
            Path.Combine(_contentRootPath, "App_Data", "documents", tenant.Id.ToString()), // ADR-083
            Path.Combine(_contentRootPath, "App_Data", "documents"),                       // original, shared
        ];

        var storageKeys = new List<string>();
        await using (var command = new NpgsqlCommand("SELECT storage_key FROM documents.documents WHERE storage_key IS NOT NULL", connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                storageKeys.Add(reader.GetString(0));
            }
        }

        int moved = 0, alreadyDone = 0, missing = 0;
        foreach (var storageKey in storageKeys)
        {
            if (storageKey != Path.GetFileName(storageKey))
            {
                _logger.LogWarning("  Documents: storage key '{StorageKey}' is not a bare file name — left untouched.", storageKey);
                continue;
            }

            var newPath = Path.Combine(targetFolder, storageKey);
            if (File.Exists(newPath))
            {
                alreadyDone++;
                continue;
            }

            var oldPath = previousFolders.Select(folder => Path.Combine(folder, storageKey)).FirstOrDefault(File.Exists);
            if (oldPath is null)
            {
                missing++;
                continue;
            }

            if (!dryRun)
            {
                Directory.CreateDirectory(targetFolder);
                File.Move(oldPath, newPath);
            }

            moved++;
        }

        _logger.LogInformation(
            "  Documents: {Moved} to move, {AlreadyDone} already migrated, {Missing} had no file on disk (of {Total} total).",
            moved, alreadyDone, missing, storageKeys.Count);
    }

    /// <summary>
    /// Every kind that stores its full relative path in a column: moves the file to the path
    /// <see cref="TenantFileLocations.ToCurrentPublicPath"/> maps it to, then rewrites the
    /// column. <paramref name="table"/> and <paramref name="column"/> are constants from
    /// RunAsync, never user input.
    /// </summary>
    private async Task MigratePathColumnAsync(
        NpgsqlConnection connection,
        TenantInfo tenant,
        bool dryRun,
        CancellationToken cancellationToken,
        string label,
        string kind,
        string table,
        string column,
        (string From, string To)? renameFolder = null)
    {
        var rows = new List<(Guid Id, string Path)>();
        await using (var command = new NpgsqlCommand($"SELECT id, {column} FROM {table} WHERE {column} IS NOT NULL", connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add((reader.GetGuid(0), reader.GetString(1)));
            }
        }

        int moved = 0, alreadyDone = 0, missing = 0;
        foreach (var (id, relativePath) in rows)
        {
            var newRelativePath = TenantFileLocations.ToCurrentPublicPath(relativePath, tenant.Id, kind, renameFolder);
            if (newRelativePath is null)
            {
                _logger.LogWarning("  {Label}: row {Id} has an unrecognized path shape '{Path}' — left untouched.", label, id, relativePath);
                continue;
            }

            if (newRelativePath == relativePath)
            {
                alreadyDone++;
                continue;
            }

            var oldFullPath = TenantFileLocations.PublicFullPath(_contentRootPath, relativePath);
            var newFullPath = TenantFileLocations.PublicFullPath(_contentRootPath, newRelativePath);

            // A file already at the new path means an earlier, interrupted run moved it but
            // didn't get to the column — only the column is left to fix.
            if (!File.Exists(newFullPath))
            {
                if (!File.Exists(oldFullPath))
                {
                    missing++;
                    continue;
                }

                if (!dryRun)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(newFullPath)!);
                    File.Move(oldFullPath, newFullPath, overwrite: false);
                }
            }

            if (!dryRun)
            {
                await using var updateCommand = new NpgsqlCommand($"UPDATE {table} SET {column} = @newPath WHERE id = @id", connection);
                updateCommand.Parameters.AddWithValue("newPath", newRelativePath);
                updateCommand.Parameters.AddWithValue("id", id);
                await updateCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            moved++;
        }

        _logger.LogInformation(
            "  {Label}: {Moved} to move, {AlreadyDone} already migrated, {Missing} had no file on disk (of {Total} total).",
            label, moved, alreadyDone, missing, rows.Count);
    }

    /// <summary>
    /// Deletes this tenant's ADR-083 folders once they hold no files. Only ever empty
    /// directories, and only the ones named after this tenant — the original shared folders
    /// may still hold other tenants' files (or orphans) and are left for an operator.
    /// </summary>
    private void RemoveEmptyPreviousFolders(TenantInfo tenant)
    {
        var tenantSegment = tenant.Id.ToString();
        string[] folders =
        [
            Path.Combine(_contentRootPath, "App_Data", "documents", tenantSegment),
            Path.Combine(_contentRootPath, "wwwroot", "uploads", TenantFileLocations.Consultants, tenantSegment),
            Path.Combine(_contentRootPath, "wwwroot", "uploads", TenantFileLocations.Users, tenantSegment),
            Path.Combine(_contentRootPath, "wwwroot", "uploads", TenantFileLocations.Products, tenantSegment),
            Path.Combine(_contentRootPath, "wwwroot", "uploads", TenantFileLocations.Branding, tenantSegment),
        ];

        foreach (var folder in folders)
        {
            try
            {
                RemoveIfEmpty(folder);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "  Could not remove empty folder {Folder} — harmless, remove it by hand.", folder);
            }
        }
    }

    private static void RemoveIfEmpty(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (var child in Directory.GetDirectories(directory))
        {
            RemoveIfEmpty(child);
        }

        if (!Directory.EnumerateFileSystemEntries(directory).Any())
        {
            Directory.Delete(directory);
        }
    }
}
