using FluentAssertions;
using HMS.Modules.Branding.Contracts;
using HMS.Modules.Branding.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Hosting;
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

        _sut = new BrandingLogoStorage(environment, _tenantContext);
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
    public async Task SaveAsync_WritesUnderTheCurrentTenantsOwnLogoFolder()
    {
        var tenantId = Guid.NewGuid();
        _tenantContext.SetTenant(tenantId, "irrelevant-connection-string");
        using var content = new MemoryStream([1, 2, 3]);

        var relativePath = await _sut.SaveAsync("logo.png", content, CancellationToken.None);

        relativePath.Should().StartWith($"uploads/branding/{tenantId}/logo/");
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

        relativePath.Should().StartWith($"uploads/branding/{tenantId}/favicon/");
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
}
