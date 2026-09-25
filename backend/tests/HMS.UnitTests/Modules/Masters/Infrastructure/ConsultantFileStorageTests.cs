using FluentAssertions;
using HMS.Modules.Masters.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Masters.Infrastructure;

public class ConsultantFileStorageTests : IDisposable
{
    private readonly string _contentRoot;
    private readonly TenantContext _tenantContext = new();
    private readonly ConsultantFileStorage _sut;

    public ConsultantFileStorageTests()
    {
        _contentRoot = Path.Combine(Path.GetTempPath(), "hms-consultant-file-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_contentRoot);

        var environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(_contentRoot);

        _sut = new ConsultantFileStorage(environment, _tenantContext);
    }

    public void Dispose()
    {
        if (Directory.Exists(_contentRoot))
        {
            Directory.Delete(_contentRoot, recursive: true);
        }
    }

    [Fact]
    public async Task SavePhotoAsync_WithNoTenantResolved_Throws()
    {
        var consultantId = Guid.NewGuid();
        using var content = new MemoryStream([1, 2, 3]);

        var act = () => _sut.SavePhotoAsync(consultantId, "photo.jpg", content, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SavePhotoAsync_WritesUnderTheCurrentTenantsOwnFolder()
    {
        var tenantId = Guid.NewGuid();
        _tenantContext.SetTenant(tenantId, "irrelevant-connection-string");
        var consultantId = Guid.NewGuid();
        using var content = new MemoryStream([1, 2, 3]);

        var relativePath = await _sut.SavePhotoAsync(consultantId, "photo.jpg", content, CancellationToken.None);

        relativePath.Should().Be($"uploads/Tenant/{tenantId}/consultants/{consultantId}.jpg");
        var fullPath = Path.Combine(_contentRoot, "wwwroot", "uploads", "Tenant", tenantId.ToString(), "consultants", $"{consultantId}.jpg");
        File.Exists(fullPath).Should().BeTrue();
    }

    [Fact]
    public async Task SavePhotoAsync_KeepsTwoTenantsPhotosApart_EvenForTheSameConsultantId()
    {
        // Cross-tenant id collisions aren't realistic with GUIDs, but this pins down the
        // actual isolation guarantee: two tenants writing under the same entity id must never
        // land in the same file — see the per-tenant file storage fix in DecisionLog.md.
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var consultantId = Guid.NewGuid();

        _tenantContext.SetTenant(tenantA, "conn-a");
        using (var contentA = new MemoryStream([1]))
        {
            await _sut.SavePhotoAsync(consultantId, "photo.jpg", contentA, CancellationToken.None);
        }

        _tenantContext.SetTenant(tenantB, "conn-b");
        using (var contentB = new MemoryStream([2]))
        {
            await _sut.SavePhotoAsync(consultantId, "photo.jpg", contentB, CancellationToken.None);
        }

        var pathA = Path.Combine(_contentRoot, "wwwroot", "uploads", "Tenant", tenantA.ToString(), "consultants", $"{consultantId}.jpg");
        var pathB = Path.Combine(_contentRoot, "wwwroot", "uploads", "Tenant", tenantB.ToString(), "consultants", $"{consultantId}.jpg");

        File.ReadAllBytes(pathA).Should().Equal(1);
        File.ReadAllBytes(pathB).Should().Equal(2);
    }
}
