using FluentAssertions;
using HMS.Modules.Backups.Configuration;
using HMS.Modules.Backups.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace HMS.UnitTests.Modules.Backups.Infrastructure;

public class FileSystemBackupStorageTests : IDisposable
{
    private readonly string _root;
    private readonly FileSystemBackupStorage _sut;

    public FileSystemBackupStorageTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "hms-backup-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_root);
        var options = Options.Create(new BackupOptions { BackupRootDirectory = _root, RetentionDays = 14 });
        _sut = new FileSystemBackupStorage(options, NullLogger<FileSystemBackupStorage>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void ResolveOutputPath_BuildsDateFolderThenKeyDotDump()
    {
        var path = _sut.ResolveOutputPath("masters", new DateOnly(2026, 9, 10));

        path.Should().Be(Path.Combine(_root, "2026-09-10", "masters.dump"));
    }

    [Fact]
    public async Task GetLatestAsync_ReturnsNull_WhenNoBackupsExistYet()
    {
        var latest = await _sut.GetLatestAsync("masters", CancellationToken.None);

        latest.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestAsync_ReturnsTheNewestDateThatHasThisKey()
    {
        WriteBackupFile("masters", new DateOnly(2026, 9, 8), "old");
        WriteBackupFile("masters", new DateOnly(2026, 9, 10), "newest");
        WriteBackupFile("masters", new DateOnly(2026, 9, 9), "middle");

        var latest = await _sut.GetLatestAsync("masters", CancellationToken.None);

        latest.Should().NotBeNull();
        latest!.Date.Should().Be(new DateOnly(2026, 9, 10));
        latest.SizeBytes.Should().Be("newest".Length);
    }

    [Fact]
    public async Task GetLatestAsync_FallsBackToAnOlderDate_WhenTheNewestDateIsMissingThisKey()
    {
        // e.g. a day where masters dumped fine but this one tenant's pg_dump failed — that
        // tenant's own last good backup should still be found, not "nothing available".
        WriteBackupFile("tenant-a", new DateOnly(2026, 9, 9), "yesterday-ok");
        Directory.CreateDirectory(Path.Combine(_root, "2026-09-10")); // today's folder exists, but no tenant-a.dump inside it

        var latest = await _sut.GetLatestAsync("tenant-a", CancellationToken.None);

        latest.Should().NotBeNull();
        latest!.Date.Should().Be(new DateOnly(2026, 9, 9));
    }

    [Fact]
    public async Task GetLatestAsync_IgnoresNonDateNamedFolders()
    {
        Directory.CreateDirectory(Path.Combine(_root, "not-a-date"));
        File.WriteAllText(Path.Combine(_root, "not-a-date", "masters.dump"), "should-not-count");

        var latest = await _sut.GetLatestAsync("masters", CancellationToken.None);

        latest.Should().BeNull();
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_DeletesFoldersPastRetention_KeepsRecentOnes()
    {
        var options = Options.Create(new BackupOptions { BackupRootDirectory = _root, RetentionDays = 14 });
        var sut = new FileSystemBackupStorage(options, NullLogger<FileSystemBackupStorage>.Instance);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var oldFolder = WriteBackupFile("masters", today.AddDays(-20), "old");
        var recentFolder = WriteBackupFile("masters", today.AddDays(-5), "recent");

        await sut.CleanupOldBackupsAsync(CancellationToken.None);

        Directory.Exists(oldFolder).Should().BeFalse();
        Directory.Exists(recentFolder).Should().BeTrue();
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_LeavesNonDateFoldersAlone()
    {
        var strayFolder = Path.Combine(_root, "not-a-date-folder");
        Directory.CreateDirectory(strayFolder);

        await _sut.CleanupOldBackupsAsync(CancellationToken.None);

        Directory.Exists(strayFolder).Should().BeTrue();
    }

    private string WriteBackupFile(string key, DateOnly date, string content)
    {
        var folder = Path.Combine(_root, date.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, $"{key}.dump"), content);
        return folder;
    }
}
