using FluentAssertions;
using HMS.Shared.Kernel;
using Xunit;

namespace HMS.UnitTests.Shared;

public class TenantFileLocationsTests
{
    private static readonly Guid TenantId = Guid.Parse("01a06312-c7be-7c58-af73-a47657a000d0");
    private static readonly Guid OtherTenantId = Guid.Parse("01a07a57-cd0c-7a83-a143-24b0808d016f");

    [Fact]
    public void Directories_PutEveryKindUnderTheTenantsOwnFolder()
    {
        var root = Path.Combine("C:", "app");

        TenantFileLocations.PublicDirectory(root, TenantId, TenantFileLocations.Users)
            .Should().Be(Path.Combine(root, "wwwroot", "uploads", "Tenant", TenantId.ToString(), "users"));
        TenantFileLocations.PrivateDirectory(root, TenantId, TenantFileLocations.Documents)
            .Should().Be(Path.Combine(root, "App_Data", "Tenant", TenantId.ToString(), "documents"));
    }

    [Fact]
    public void PublicRelativePath_IsAForwardSlashUrlPath()
    {
        TenantFileLocations.PublicRelativePath(TenantId, TenantFileLocations.Products, "p1", "images", "a.png")
            .Should().Be($"uploads/Tenant/{TenantId}/products/p1/images/a.png");
    }

    [Theory]
    [InlineData("uploads/Tenant/{tenant}/branding/primary/a.png", true)]
    [InlineData("uploads/Tenant/{other}/branding/primary/a.png", false)] // another tenant
    [InlineData("uploads/Tenant/{tenant}/users/a.png", false)]           // another kind
    [InlineData("uploads/branding/{tenant}/logo/a.png", false)]          // ADR-083 layout
    [InlineData("uploads/Tenant/{tenant}/branding/../users/a.png", false)]
    [InlineData("uploads/Tenant/{tenant}/branding/", false)]
    [InlineData("uploads\\Tenant\\{tenant}\\branding\\a.png", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsInTenantPublicFolder_OnlyAcceptsThisTenantsOwnKindFolder(string? path, bool expected)
    {
        TenantFileLocations.IsInTenantPublicFolder(Expand(path), TenantId, TenantFileLocations.Branding)
            .Should().Be(expected);
    }

    [Theory]
    [InlineData("uploads/users/u1.jpg", "uploads/Tenant/{tenant}/users/u1.jpg")]                     // original
    [InlineData("uploads/users/{tenant}/u1.jpg", "uploads/Tenant/{tenant}/users/u1.jpg")]            // ADR-083
    [InlineData("uploads/Tenant/{tenant}/users/u1.jpg", "uploads/Tenant/{tenant}/users/u1.jpg")]     // current
    [InlineData("uploads/consultants/c1.png", null)]                                                  // another kind
    [InlineData("uploads/Tenant/{other}/users/u1.jpg", null)]                                         // another tenant
    [InlineData("uploads/users/{tenant}", null)]                                                      // no file
    [InlineData("uploads/users/../secret.jpg", null)]
    [InlineData("https://example.com/u1.jpg", null)]
    public void ToCurrentPublicPath_MapsEveryOlderLayout(string path, string? expected)
    {
        TenantFileLocations.ToCurrentPublicPath(Expand(path)!, TenantId, TenantFileLocations.Users)
            .Should().Be(Expand(expected));
    }

    [Theory]
    [InlineData("uploads/products/p1/images/a.png")]
    [InlineData("uploads/products/{tenant}/p1/images/a.png")]
    public void ToCurrentPublicPath_KeepsAProductsOwnSubfolders(string path)
    {
        TenantFileLocations.ToCurrentPublicPath(Expand(path)!, TenantId, TenantFileLocations.Products)
            .Should().Be($"uploads/Tenant/{TenantId}/products/p1/images/a.png");
    }

    [Theory]
    [InlineData("uploads/branding/logo/a.png")]
    [InlineData("uploads/branding/{tenant}/logo/a.png")]
    [InlineData("uploads/Tenant/{tenant}/branding/logo/a.png")]
    [InlineData("uploads/Tenant/{tenant}/branding/primary/a.png")]
    public void ToCurrentPublicPath_RenamesThePrimaryLogoFolder(string path)
    {
        TenantFileLocations.ToCurrentPublicPath(Expand(path)!, TenantId, TenantFileLocations.Branding, ("logo", "primary"))
            .Should().Be($"uploads/Tenant/{TenantId}/branding/primary/a.png");
    }

    [Fact]
    public void ToCurrentPublicPath_LeavesOtherLogoSlotsFoldersAsTheyAre()
    {
        TenantFileLocations.ToCurrentPublicPath($"uploads/branding/{TenantId}/favicon/a.png", TenantId, TenantFileLocations.Branding)
            .Should().Be($"uploads/Tenant/{TenantId}/branding/favicon/a.png");
    }

    private static string? Expand(string? path)
        => path?.Replace("{tenant}", TenantId.ToString()).Replace("{other}", OtherTenantId.ToString());
}
