using FluentAssertions;
using HMS.Modules.Identity.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Identity.Infrastructure;

public class UserFileStorageTests : IDisposable
{
    private readonly string _contentRoot;
    private readonly TenantContext _tenantContext = new();
    private readonly UserFileStorage _sut;

    public UserFileStorageTests()
    {
        _contentRoot = Path.Combine(Path.GetTempPath(), "hms-user-file-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_contentRoot);

        var environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(_contentRoot);

        _sut = new UserFileStorage(environment, _tenantContext, NullLogger<UserFileStorage>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_contentRoot))
        {
            Directory.Delete(_contentRoot, recursive: true);
        }
    }

    [Fact]
    public async Task SaveProfilePhotoAsync_WithNoTenantResolved_Throws()
    {
        var userId = Guid.NewGuid();
        using var content = new MemoryStream([1, 2, 3]);

        var act = () => _sut.SaveProfilePhotoAsync(userId, "photo.jpg", content, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SaveProfilePhotoAsync_WritesUnderTheCurrentTenantsOwnFolder()
    {
        var tenantId = Guid.NewGuid();
        _tenantContext.SetTenant(tenantId, "irrelevant-connection-string");
        var userId = Guid.NewGuid();
        using var content = new MemoryStream([1, 2, 3]);

        var relativePath = await _sut.SaveProfilePhotoAsync(userId, "photo.jpg", content, CancellationToken.None);

        relativePath.Should().Be($"uploads/Tenant/{tenantId}/users/{userId}.jpg");
        var fullPath = Path.Combine(_contentRoot, "wwwroot", "uploads", "Tenant", tenantId.ToString(), "users", $"{userId}.jpg");
        File.Exists(fullPath).Should().BeTrue();
    }

    [Fact]
    public async Task SaveProfilePhotoAsync_KeepsTwoTenantsPhotosApart_EvenForTheSameUserId()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _tenantContext.SetTenant(tenantA, "conn-a");
        using (var contentA = new MemoryStream([1]))
        {
            await _sut.SaveProfilePhotoAsync(userId, "photo.jpg", contentA, CancellationToken.None);
        }

        _tenantContext.SetTenant(tenantB, "conn-b");
        using (var contentB = new MemoryStream([2]))
        {
            await _sut.SaveProfilePhotoAsync(userId, "photo.jpg", contentB, CancellationToken.None);
        }

        var pathA = Path.Combine(_contentRoot, "wwwroot", "uploads", "Tenant", tenantA.ToString(), "users", $"{userId}.jpg");
        var pathB = Path.Combine(_contentRoot, "wwwroot", "uploads", "Tenant", tenantB.ToString(), "users", $"{userId}.jpg");

        File.ReadAllBytes(pathA).Should().Equal(1);
        File.ReadAllBytes(pathB).Should().Equal(2);
    }

    [Fact]
    public async Task DeleteAsync_RemovesAPhotoFromTheTenantsOwnFolder()
    {
        _tenantContext.SetTenant(Guid.NewGuid(), "conn");
        using var content = new MemoryStream([1]);
        var relativePath = await _sut.SaveProfilePhotoAsync(Guid.NewGuid(), "photo.jpg", content, CancellationToken.None);

        await _sut.DeleteAsync(relativePath, CancellationToken.None);

        File.Exists(FullPath(relativePath)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_LeavesAnotherTenantsPhotoAndOlderLayoutsAlone()
    {
        var tenantA = Guid.NewGuid();
        _tenantContext.SetTenant(tenantA, "conn-a");
        using var content = new MemoryStream([1]);
        var tenantAsPhoto = await _sut.SaveProfilePhotoAsync(Guid.NewGuid(), "photo.jpg", content, CancellationToken.None);
        var olderLayoutPhoto = WriteFile($"uploads/users/{tenantA}/old.jpg");

        _tenantContext.SetTenant(Guid.NewGuid(), "conn-b");
        await _sut.DeleteAsync(tenantAsPhoto, CancellationToken.None);
        _tenantContext.SetTenant(tenantA, "conn-a");
        await _sut.DeleteAsync(olderLayoutPhoto, CancellationToken.None);

        File.Exists(FullPath(tenantAsPhoto)).Should().BeTrue();
        File.Exists(FullPath(olderLayoutPhoto)).Should().BeTrue();
    }

    private string FullPath(string relativePath)
        => Path.Combine(_contentRoot, "wwwroot", relativePath.Replace('/', Path.DirectorySeparatorChar));

    private string WriteFile(string relativePath)
    {
        var fullPath = FullPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllBytes(fullPath, [1]);
        return relativePath;
    }
}
