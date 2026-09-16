using FluentAssertions;
using HMS.Modules.Identity.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Hosting;
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

        _sut = new UserFileStorage(environment, _tenantContext);
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

        relativePath.Should().Be($"uploads/users/{tenantId}/{userId}.jpg");
        var fullPath = Path.Combine(_contentRoot, "wwwroot", "uploads", "users", tenantId.ToString(), $"{userId}.jpg");
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

        var pathA = Path.Combine(_contentRoot, "wwwroot", "uploads", "users", tenantA.ToString(), $"{userId}.jpg");
        var pathB = Path.Combine(_contentRoot, "wwwroot", "uploads", "users", tenantB.ToString(), $"{userId}.jpg");

        File.ReadAllBytes(pathA).Should().Equal(1);
        File.ReadAllBytes(pathB).Should().Equal(2);
    }
}
