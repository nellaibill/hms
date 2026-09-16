using HMS.Modules.Platform.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace HMS.Api.Provisioning;

/// <summary>
/// One-time, explicitly-invoked (never automatic) fixup for the per-tenant file storage
/// change: every upload used to land in one shared directory tree regardless of which
/// hospital it belonged to (see docs/DecisionLog.md ADR-083). This
/// walks every active tenant and, for each of the five upload kinds, moves any file still
/// sitting at its old un-scoped path into that tenant's own subfolder — rewriting the
/// stored path column too, for the four kinds (everything except Documents' StorageKey,
/// which was never a full path) where the column itself carries the path.
///
/// Idempotent and safe to re-run: anything already at its new tenant-scoped shape is left
/// alone, so a partially-completed or repeated run just picks up where it left off. One
/// tenant/one row failing is logged and skipped rather than aborting the whole run — same
/// "don't let one bad row take down the batch" posture as DailyBackupSchedulerService.
///
/// Invoked via `dotnet HMS.Api.dll migrate-tenant-files [--dry-run]` (see Program.cs) —
/// deliberately a separate, explicitly-typed command from the schema `migrate` step, and
/// deliberately not folded into Development's auto-migrate-on-startup, because unlike a
/// schema migration this one moves real files and rewrites real rows; an operator should
/// run `--dry-run` first, read the counts, and only then run it for real.
/// </summary>
public sealed class TenantFileStorageMigrator
{
    private readonly string _appDataRoot;
    private readonly string _wwwrootRoot;
    private readonly ILogger<TenantFileStorageMigrator> _logger;

    public TenantFileStorageMigrator(IHostEnvironment environment, ILogger<TenantFileStorageMigrator> logger)
    {
        _appDataRoot = Path.Combine(environment.ContentRootPath, "App_Data");
        _wwwrootRoot = Path.Combine(environment.ContentRootPath, "wwwroot");
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
                // query runs — that must skip only this one kind, not abandon the other four
                // (found live: a tenant missing products.product_images otherwise also lost
                // its Branding logo check, which has nothing to do with Products).
                await RunKindAsync(tenant, "Documents", () => MigrateDocumentsAsync(connection, tenant, dryRun, cancellationToken));
                await RunKindAsync(tenant, "consultant photos", () => MigrateSingleSlotColumnAsync(
                    connection, tenant, dryRun, cancellationToken,
                    label: "consultant photos",
                    selectSql: "SELECT id, photo_url FROM masters.consultants WHERE photo_url IS NOT NULL",
                    updateSql: "UPDATE masters.consultants SET photo_url = @newPath WHERE id = @id",
                    legacySegmentCount: 3,
                    scopedSegmentCount: 4));
                await RunKindAsync(tenant, "user photos", () => MigrateSingleSlotColumnAsync(
                    connection, tenant, dryRun, cancellationToken,
                    label: "user photos",
                    selectSql: "SELECT id, profile_photo_url FROM identity.users WHERE profile_photo_url IS NOT NULL",
                    updateSql: "UPDATE identity.users SET profile_photo_url = @newPath WHERE id = @id",
                    legacySegmentCount: 3,
                    scopedSegmentCount: 4));
                await RunKindAsync(tenant, "Product images", () => MigrateProductImagesAsync(connection, tenant, dryRun, cancellationToken));
                await RunKindAsync(tenant, "Branding logo", () => MigrateBrandingLogoAsync(connection, tenant, dryRun, cancellationToken));
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
    /// four kinds — see the "Each kind runs in its own try/catch" note in RunAsync above for
    /// why this matters (a tenant missing one module's schema entirely is a real, expected
    /// case, not a bug).
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
    /// ("{documentId}{ext}") — DocumentFileStorage now resolves the tenant folder itself from
    /// ITenantContext at read/write time, so no DB rewrite is needed, only the physical move.
    /// </summary>
    private async Task MigrateDocumentsAsync(NpgsqlConnection connection, TenantInfo tenant, bool dryRun, CancellationToken cancellationToken)
    {
        var oldRoot = Path.Combine(_appDataRoot, "documents");
        var newRoot = Path.Combine(_appDataRoot, "documents", tenant.Id.ToString());

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
            var oldPath = Path.Combine(oldRoot, storageKey);
            var newPath = Path.Combine(newRoot, storageKey);

            if (File.Exists(newPath))
            {
                alreadyDone++;
                continue;
            }

            if (!File.Exists(oldPath))
            {
                missing++;
                continue;
            }

            if (!dryRun)
            {
                Directory.CreateDirectory(newRoot);
                File.Move(oldPath, newPath);
            }

            moved++;
        }

        _logger.LogInformation(
            "  Documents: {Moved} to move, {AlreadyDone} already migrated, {Missing} had no file on disk (of {Total} total).",
            moved, alreadyDone, missing, storageKeys.Count);
    }

    /// <summary>
    /// Shared shape for Consultant photos and User photos: a single "uploads/{kind}/{id}.ext"
    /// column, moving to "uploads/{kind}/{tenantId}/{id}.ext". Detected as already-migrated by
    /// segment count rather than re-parsing the tenant id out of the path, so a stray legacy
    /// path that happens to collide is never misread as already-done.
    /// </summary>
    private async Task MigrateSingleSlotColumnAsync(
        NpgsqlConnection connection,
        TenantInfo tenant,
        bool dryRun,
        CancellationToken cancellationToken,
        string label,
        string selectSql,
        string updateSql,
        int legacySegmentCount,
        int scopedSegmentCount)
    {
        var rows = new List<(Guid Id, string Path)>();
        await using (var command = new NpgsqlCommand(selectSql, connection))
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
            var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length == scopedSegmentCount)
            {
                alreadyDone++;
                continue;
            }

            if (segments.Length != legacySegmentCount)
            {
                _logger.LogWarning("  {Label}: row {Id} has an unrecognized path shape '{Path}' — left untouched.", label, id, relativePath);
                continue;
            }

            // "uploads/{kind}/{filename}" -> "uploads/{kind}/{tenantId}/{filename}"
            var fileName = segments[^1];
            var newRelativePath = string.Join('/', segments[..^1].Append(tenant.Id.ToString()).Append(fileName));

            var oldFullPath = Path.Combine(_wwwrootRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var newFullPath = Path.Combine(_wwwrootRoot, newRelativePath.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(oldFullPath))
            {
                missing++;
                continue;
            }

            if (!dryRun)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(newFullPath)!);
                File.Move(oldFullPath, newFullPath, overwrite: false);

                await using var updateCommand = new NpgsqlCommand(updateSql, connection);
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
    /// Product images: "uploads/products/{productId}/images/{file}" -&gt;
    /// "uploads/products/{tenantId}/{productId}/images/{file}" — one extra segment inserted
    /// after "products" rather than appended at the end, unlike the single-slot columns above.
    /// </summary>
    private async Task MigrateProductImagesAsync(NpgsqlConnection connection, TenantInfo tenant, bool dryRun, CancellationToken cancellationToken)
    {
        var rows = new List<(Guid Id, string Path)>();
        await using (var command = new NpgsqlCommand("SELECT id, image_url FROM products.product_images WHERE image_url IS NOT NULL", connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add((reader.GetGuid(0), reader.GetString(1)));
            }
        }

        const int legacySegmentCount = 5; // uploads/products/{productId}/images/{file}
        const int scopedSegmentCount = 6; // uploads/products/{tenantId}/{productId}/images/{file}

        int moved = 0, alreadyDone = 0, missing = 0;
        foreach (var (id, relativePath) in rows)
        {
            var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length == scopedSegmentCount)
            {
                alreadyDone++;
                continue;
            }

            if (segments.Length != legacySegmentCount)
            {
                _logger.LogWarning("  Product images: row {Id} has an unrecognized path shape '{Path}' — left untouched.", id, relativePath);
                continue;
            }

            var newSegments = new[] { segments[0], segments[1], tenant.Id.ToString(), segments[2], segments[3], segments[4] };
            var newRelativePath = string.Join('/', newSegments);

            var oldFullPath = Path.Combine(_wwwrootRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var newFullPath = Path.Combine(_wwwrootRoot, newRelativePath.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(oldFullPath))
            {
                missing++;
                continue;
            }

            if (!dryRun)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(newFullPath)!);
                File.Move(oldFullPath, newFullPath, overwrite: false);

                await using var updateCommand = new NpgsqlCommand("UPDATE products.product_images SET image_url = @newPath WHERE id = @id", connection);
                updateCommand.Parameters.AddWithValue("newPath", newRelativePath);
                updateCommand.Parameters.AddWithValue("id", id);
                await updateCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            moved++;
        }

        _logger.LogInformation(
            "  Product images: {Moved} to move, {AlreadyDone} already migrated, {Missing} had no file on disk (of {Total} total).",
            moved, alreadyDone, missing, rows.Count);
    }

    /// <summary>
    /// Branding logo: "uploads/branding/logo/{file}" -&gt; "uploads/branding/{tenantId}/logo/{file}"
    /// — at most one row (BrandingSettings is a singleton table per tenant database).
    /// </summary>
    private async Task MigrateBrandingLogoAsync(NpgsqlConnection connection, TenantInfo tenant, bool dryRun, CancellationToken cancellationToken)
    {
        var rows = new List<(Guid Id, string Path)>();
        await using (var command = new NpgsqlCommand("SELECT id, logo_path FROM branding.branding_settings WHERE logo_path IS NOT NULL", connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add((reader.GetGuid(0), reader.GetString(1)));
            }
        }

        const int legacySegmentCount = 4; // uploads/branding/logo/{file}
        const int scopedSegmentCount = 5; // uploads/branding/{tenantId}/logo/{file}

        int moved = 0, alreadyDone = 0, missing = 0;
        foreach (var (id, relativePath) in rows)
        {
            // Kept as its own branch (rather than reusing MigrateSingleSlotColumnAsync)
            // because the inserted tenant segment goes before "logo", not after the whole path.
            var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length == scopedSegmentCount)
            {
                alreadyDone++;
                continue;
            }

            if (segments.Length != legacySegmentCount)
            {
                _logger.LogWarning("  Branding logo: row {Id} has an unrecognized path shape '{Path}' — left untouched.", id, relativePath);
                continue;
            }

            // segments: ["uploads", "branding", "logo", "{file}"] -> insert tenantId before "logo"
            var newRelativePath = string.Join('/', segments[0], segments[1], tenant.Id.ToString(), segments[2], segments[3]);

            var oldFullPath = Path.Combine(_wwwrootRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var newFullPath = Path.Combine(_wwwrootRoot, newRelativePath.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(oldFullPath))
            {
                missing++;
                continue;
            }

            if (!dryRun)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(newFullPath)!);
                File.Move(oldFullPath, newFullPath, overwrite: false);

                await using var updateCommand = new NpgsqlCommand("UPDATE branding.branding_settings SET logo_path = @newPath WHERE id = @id", connection);
                updateCommand.Parameters.AddWithValue("newPath", newRelativePath);
                updateCommand.Parameters.AddWithValue("id", id);
                await updateCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            moved++;
        }

        _logger.LogInformation(
            "  Branding logo: {Moved} to move, {AlreadyDone} already migrated, {Missing} had no file on disk (of {Total} total).",
            moved, alreadyDone, missing, rows.Count);
    }
}
