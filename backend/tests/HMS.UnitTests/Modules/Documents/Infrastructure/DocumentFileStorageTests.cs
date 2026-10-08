using FluentAssertions;
using HMS.Modules.Documents.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Documents.Infrastructure;

public class DocumentFileStorageTests : IDisposable
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly string _contentRoot;
    private readonly TenantContext _tenantContext = new();
    private readonly DocumentFileStorage _sut;

    public DocumentFileStorageTests()
    {
        _contentRoot = Path.Combine(Path.GetTempPath(), "hms-document-file-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_contentRoot);

        var environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(_contentRoot);

        _tenantContext.SetTenant(_tenantId, "irrelevant-connection-string");
        _sut = new DocumentFileStorage(environment, _tenantContext);
    }

    public void Dispose()
    {
        if (Directory.Exists(_contentRoot))
        {
            Directory.Delete(_contentRoot, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_WritesUnderTheTenantsOwnPrivateFolder()
    {
        var documentId = Guid.NewGuid();
        using var content = new MemoryStream([1, 2, 3]);

        var saved = await _sut.SaveAsync(documentId, "report.pdf", content, CancellationToken.None);

        saved.StorageKey.Should().Be($"{documentId}.pdf");
        File.Exists(Path.Combine(_contentRoot, "App_Data", "Tenant", _tenantId.ToString(), "documents", saved.StorageKey)).Should().BeTrue();
    }

    [Fact]
    public async Task OpenReadAsync_FallsBackToTheAdr083Folder_UntilTheFileIsMigrated()
    {
        var previousPath = Path.Combine(_contentRoot, "App_Data", "documents", _tenantId.ToString(), "old.pdf");
        Directory.CreateDirectory(Path.GetDirectoryName(previousPath)!);
        await File.WriteAllBytesAsync(previousPath, [7, 8]);

        await using var stream = await _sut.OpenReadAsync("old.pdf", CancellationToken.None);
        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy);

        copy.ToArray().Should().Equal(7, 8);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheFileFromEitherFolder()
    {
        using var content = new MemoryStream([1]);
        var saved = await _sut.SaveAsync(Guid.NewGuid(), "new.pdf", content, CancellationToken.None);
        var previousPath = Path.Combine(_contentRoot, "App_Data", "documents", _tenantId.ToString(), "old.pdf");
        Directory.CreateDirectory(Path.GetDirectoryName(previousPath)!);
        await File.WriteAllBytesAsync(previousPath, [1]);

        await _sut.DeleteAsync(saved.StorageKey, CancellationToken.None);
        await _sut.DeleteAsync("old.pdf", CancellationToken.None);

        File.Exists(Path.Combine(_contentRoot, "App_Data", "Tenant", _tenantId.ToString(), "documents", saved.StorageKey)).Should().BeFalse();
        File.Exists(previousPath).Should().BeFalse();
    }
}
