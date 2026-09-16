using HMS.Modules.Platform.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace HMS.Api.Provisioning;

/// <summary>
/// One-time, explicitly-invoked (never automatic) fixup that lands every tenant's uploads in
/// their final tenant-first shape — "wwwroot/uploads/Tenant/{tenantId}/{kind}/…" — one
/// subfolder per upload kind underneath that tenant's own folder (see
/// docs/DecisionLog.md ADR-083). This walks every active tenant and, for each of the four
/// wwwroot-hosted upload kinds, moves any file still sitting at an older path shape into its
/// final location — rewriting the stored path column too, since every one of these four kinds
/// persists its full relative path in the database. Recognizes and moves files from either of
/// two older shapes: the very first, fully-unscoped layout ("uploads/{kind}/…", shared by every
/// tenant) and the interim kind-first-scoped layout from the previous tenant-scoping pass
/// ("uploads/{kind}/{tenantId}/…"). Documents are deliberately out of scope here — they live
/// under App_Data, outside wwwroot, specifically so they're never reachable through
/// UseStaticFiles (see DocumentFileStorage's own doc comment), and DocumentFileStorage already
/// resolves its own tenant folder at read/write time rather than persisting a path.
///
/// Idempotent and safe to re-run: anything already at its final tenant-first shape is left
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
                // query runs — that must skip only this one kind, not abandon the others
                // (found live: a tenant missing products.product_images otherwise also lost
                // its Branding logo check, which has nothing to do with Products).
                await RunKindAsync(tenant, "Documents", () => MigrateDocumentsAsync(connection, tenant, dryRun, cancellationToken));
                await RunKindAsync(tenant, "consultant photos", () => MigrateSingleSlotColumnAsync(
                    connection, tenant, dryRun, cancellationToken,
                    label: "consultant photos",
                    kind: "consultants",
                    selectSql: "SELECT id, photo_url FROM masters.consultants WHERE photo_url IS NOT NULL",
                    updateSql: "UPDATE masters.consultants SET photo_url = @newPath WHERE id = @id"));
                await RunKindAsync(tenant, "user photos", () => MigrateSingleSlotColumnAsync(
                    connection, tenant, dryRun, cancellationToken,
                    label: "user photos",
                    kind: "users",
                    selectSql: "SELECT id, profile_photo_url FROM identity.users WHERE profile_photo_url IS NOT NULL",
                    updateSql: "UPDATE identity.users SET profile_photo_url = @newPath WHERE id = @id"));
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
    /// kinds — see the "Each kind runs in its own try/catch" note in RunAsync above for
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
    /// Maps a stored relative path onto its tenant-first final shape
    /// "uploads/Tenant/{tenantId}/{kind}/{tail...}", recognizing both older shapes this
    /// migrator moves files out of: the original fully-unscoped "uploads/{kind}/{tail...}"
    /// and the interim kind-first-scoped "uploads/{kind}/{tenantId}/{tail...}" from the
    /// previous tenant-scoping pass. <paramref name="kind"/> is everything after the tenant
    /// segment and before the entity-specific tail (e.g. "consultants", or "products" for
    /// "products/{productId}/images/{file}") — the tail itself is carried through unchanged
    /// regardless of which older shape it came from, since only the segments before it differ.
    /// </summary>
    private static (bool IsAlreadyFinal, string? NewPath) ResolveTenantFirstPath(string relativePath, string kind, Guid tenantId)
    {
        var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2 || segments[0] != "uploads")
        {
            return (false, null);
        }

        if (segments[1] == "Tenant")
        {
            return (true, null);
        }

        if (segments[1] != kind)
        {
            return (false, null);
        }

        var tail = segments[2..];
        if (tail.Length > 0 && tail[0] == tenantId.ToString())
        {
            tail = tail[1..];
        }

        if (tail.Length == 0)
        {
            return (false, null);
        }

        var newSegments = new List<string> { "uploads", "Tenant", tenantId.ToString(), kind };
        newSegments.AddRange(tail);
        return (false, string.Join('/', newSegments));
    }

    /// <summary>
    /// Documents: the stored `storage_key` column is only ever a bare filename
    /// ("{documentId}{ext}") — DocumentFileStorage resolves the tenant folder itself from
    /// ITenantContext at read/write time, so no DB rewrite is needed, only the physical move.
    /// Lives under App_Data, not wwwroot — untouched by the tenant-first wwwroot reshuffle
    /// the other four kinds below go through.
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
    /// Shared shape for Consultant photos and User photos: a single "uploads/{kind}/…" column
    /// pointing at one always-replaceable slot, moving to
    /// "uploads/Tenant/{tenantId}/{kind}/{file}".
    /// </summary>
    private async Task MigrateSingleSlotColumnAsync(
        NpgsqlConnection connection,
        TenantInfo tenant,
        bool dryRun,
        CancellationToken cancellationToken,
        string label,
        string kind,
        string selectSql,
        string updateSql)
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
            var (isAlreadyFinal, newRelativePath) = ResolveTenantFirstPath(relativePath, kind, tenant.Id);

            if (isAlreadyFinal)
            {
                alreadyDone++;
                continue;
            }

            if (newRelativePath is null)
            {
                _logger.LogWarning("  {Label}: row {Id} has an unrecognized path shape '{Path}' — left untouched.", label, id, relativePath);
                continue;
            }

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
    /// Product images: "uploads/products/{productId}/images/{file}" or
    /// "uploads/products/{tenantId}/{productId}/images/{file}" -&gt;
    /// "uploads/Tenant/{tenantId}/products/{productId}/images/{file}".
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

        int moved = 0, alreadyDone = 0, missing = 0;
        foreach (var (id, relativePath) in rows)
        {
            var (isAlreadyFinal, newRelativePath) = ResolveTenantFirstPath(relativePath, "products", tenant.Id);

            if (isAlreadyFinal)
            {
                alreadyDone++;
                continue;
            }

            if (newRelativePath is null)
            {
                _logger.LogWarning("  Product images: row {Id} has an unrecognized path shape '{Path}' — left untouched.", id, relativePath);
                continue;
            }

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
    /// Branding logo: "uploads/branding/logo/{file}" or "uploads/branding/{tenantId}/logo/{file}"
    /// -&gt; "uploads/Tenant/{tenantId}/branding/logo/{file}" — at most one row (BrandingSettings
    /// is a singleton table per tenant database).
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

        int moved = 0, alreadyDone = 0, missing = 0;
        foreach (var (id, relativePath) in rows)
        {
            var (isAlreadyFinal, newRelativePath) = ResolveTenantFirstPath(relativePath, "branding", tenant.Id);

            if (isAlreadyFinal)
            {
                alreadyDone++;
                continue;
            }

            if (newRelativePath is null)
            {
                _logger.LogWarning("  Branding logo: row {Id} has an unrecognized path shape '{Path}' — left untouched.", id, relativePath);
                continue;
            }

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
