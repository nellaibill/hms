using HMS.Modules.Backups.Application.Abstractions;
using HMS.Modules.Backups.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HMS.Modules.Backups.Infrastructure;

internal sealed class FileSystemBackupStorage : IBackupStorage
{
    private const string DateFolderFormat = "yyyy-MM-dd";

    private readonly BackupOptions _options;
    private readonly ILogger<FileSystemBackupStorage> _logger;

    public FileSystemBackupStorage(IOptions<BackupOptions> options, ILogger<FileSystemBackupStorage> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string ResolveOutputPath(string key, DateOnly date)
        => Path.Combine(_options.BackupRootDirectory, date.ToString(DateFolderFormat), $"{key}.dump");

    public Task<BackupFileInfo?> GetLatestAsync(string key, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_options.BackupRootDirectory))
        {
            return Task.FromResult<BackupFileInfo?>(null);
        }

        // Date-named folders sort correctly as plain strings ("yyyy-MM-dd" is lexicographically
        // ordered the same as chronologically), so the newest folder that actually contains
        // this key's file is the most recent successful backup for it — a day where masters
        // dumped fine but one tenant's pg_dump failed still lets that tenant's own last good
        // backup show up here, rather than "the newest folder or nothing at all".
        var dateFolders = Directory.GetDirectories(_options.BackupRootDirectory)
            .Select(Path.GetFileName)
            .Where(name => name is not null && DateOnly.TryParseExact(name, DateFolderFormat, out _))
            .OrderByDescending(name => name, StringComparer.Ordinal);

        foreach (var folderName in dateFolders)
        {
            var filePath = Path.Combine(_options.BackupRootDirectory, folderName!, $"{key}.dump");
            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                var date = DateOnly.ParseExact(folderName!, DateFolderFormat);
                return Task.FromResult<BackupFileInfo?>(new BackupFileInfo(key, date, filePath, fileInfo.Length));
            }
        }

        return Task.FromResult<BackupFileInfo?>(null);
    }

    public Task CleanupOldBackupsAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_options.BackupRootDirectory))
        {
            return Task.CompletedTask;
        }

        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-_options.RetentionDays);
        foreach (var directory in Directory.GetDirectories(_options.BackupRootDirectory))
        {
            var name = Path.GetFileName(directory);
            if (name is null || !DateOnly.TryParseExact(name, DateFolderFormat, out var folderDate) || folderDate >= cutoff)
            {
                continue;
            }

            try
            {
                Directory.Delete(directory, recursive: true);
                _logger.LogInformation("Deleted backup folder past the {RetentionDays}-day retention window: {Directory}", _options.RetentionDays, directory);
            }
            catch (Exception ex)
            {
                // Not fatal to the run — an undeleted old folder just means slightly more disk
                // used, not a failed backup; logged so it isn't silently ignored forever.
                _logger.LogWarning(ex, "Failed to delete old backup folder {Directory}", directory);
            }
        }

        return Task.CompletedTask;
    }
}
