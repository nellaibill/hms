using FluentAssertions;
using HMS.Modules.Branding.Contracts;
using HMS.Modules.Branding.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Branding.Infrastructure;

public class BrandingLogoStorageTests : IDisposable
{
    private readonly string _contentRoot;
    private readonly TenantContext _tenantContext = new();
    private readonly BrandingLogoStorage _sut;

    public BrandingLogoStorageTests()
    {
        _contentRoot = Path.Combine(Path.GetTempPath(), "hms-branding-logo-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_contentRoot);

        var environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(_contentRoot);

        _sut = new BrandingLogoStorage(environment, _tenantContext, NullLogger<BrandingLogoStorage>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_contentRoot))
        {
            Directory.Delete(_contentRoot, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_WithNoTenantResolved_Throws()
    {
        using var content = new MemoryStream([1, 2, 3]);

        var act = () => _sut.SaveAsync("logo.png", content, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SaveAsync_WritesThePrimaryLogoUnderTheTenantsOwnFolder()
    {
        var tenantId = Guid.NewGuid();
        _tenantContext.SetTenant(tenantId, "irrelevant-connection-string");
        using var content = new MemoryStream([1, 2, 3]);

        var relativePath = await _sut.SaveAsync("logo.png", content, CancellationToken.None);

        relativePath.Should().StartWith($"uploads/Tenant/{tenantId}/branding/primary/").And.EndWith(".png");
        var fullPath = Path.Combine(_contentRoot, "wwwroot", relativePath.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(fullPath).Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_WritesANonPrimarySlotUnderItsOwnFolder()
    {
        var tenantId = Guid.NewGuid();
        _tenantContext.SetTenant(tenantId, "irrelevant-connection-string");
        using var content = new MemoryStream([1, 2, 3]);

        var relativePath = await _sut.SaveAsync("favicon.png", content, CancellationToken.None, BrandingLogoSlots.Favicon);

        relativePath.Should().StartWith($"uploads/Tenant/{tenantId}/branding/favicon/");
    }

    [Fact]
    public async Task SaveAsync_KeepsTwoTenantsLogosApart()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        _tenantContext.SetTenant(tenantA, "conn-a");
        using var contentA = new MemoryStream([1]);
        var pathA = await _sut.SaveAsync("logo.png", contentA, CancellationToken.None);

        _tenantContext.SetTenant(tenantB, "conn-b");
        using var contentB = new MemoryStream([2]);
        var pathB = await _sut.SaveAsync("logo.png", contentB, CancellationToken.None);

        pathA.Should().NotBe(pathB);
        pathA.Should().Contain(tenantA.ToString());
        pathB.Should().Contain(tenantB.ToString());
    }

    [Fact]
    public async Task DeleteAsync_RemovesALogoFromTheTenantsOwnFolder()
    {
        _tenantContext.SetTenant(Guid.NewGuid(), "conn");
        using var content = new MemoryStream([1]);
        var relativePath = await _sut.SaveAsync("logo.png", content, CancellationToken.None, BrandingLogoSlots.Login);

        await _sut.DeleteAsync(relativePath, CancellationToken.None);

        File.Exists(FullPath(relativePath)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_LeavesAnotherTenantsLogoAndOlderLayoutsAlone()
    {
        var tenantA = Guid.NewGuid();
        _tenantContext.SetTenant(tenantA, "conn-a");
        using var content = new MemoryStream([1]);
        var tenantAsLogo = await _sut.SaveAsync("logo.png", content, CancellationToken.None);
        var olderLayoutLogo = WriteFile($"uploads/branding/{tenantA}/logo/old.png");

        _tenantContext.SetTenant(Guid.NewGuid(), "conn-b");
        await _sut.DeleteAsync(tenantAsLogo, CancellationToken.None);
        _tenantContext.SetTenant(tenantA, "conn-a");
        await _sut.DeleteAsync(olderLayoutLogo, CancellationToken.None);
        await _sut.DeleteAsync(null, CancellationToken.None);

        File.Exists(FullPath(tenantAsLogo)).Should().BeTrue();
        File.Exists(FullPath(olderLayoutLogo)).Should().BeTrue();
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
