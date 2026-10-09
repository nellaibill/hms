using FluentAssertions;
using HMS.Modules.Products.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Products.Infrastructure;

public class ProductImageStorageTests : IDisposable
{
    private readonly string _contentRoot;
    private readonly TenantContext _tenantContext = new();
    private readonly ProductImageStorage _sut;

    public ProductImageStorageTests()
    {
        _contentRoot = Path.Combine(Path.GetTempPath(), "hms-product-image-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_contentRoot);

        var environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(_contentRoot);

        _sut = new ProductImageStorage(environment, _tenantContext);
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

        var act = () => _sut.SaveAsync(Guid.NewGuid(), "image.png", content, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SaveAsync_WritesUnderTheProductsFolderInsideTheTenantsOwnFolder()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        _tenantContext.SetTenant(tenantId, "irrelevant-connection-string");
        using var content = new MemoryStream([1, 2, 3]);

        var relativePath = await _sut.SaveAsync(productId, "image.png", content, CancellationToken.None);

        relativePath.Should().StartWith($"uploads/Tenant/{tenantId}/products/{productId}/images/").And.EndWith(".png");
        File.Exists(Path.Combine(_contentRoot, "wwwroot", relativePath.Replace('/', Path.DirectorySeparatorChar))).Should().BeTrue();
    }
}
