using FluentAssertions;
using HMS.Modules.Branding.Application;
using HMS.Modules.Branding.Contracts;
using HMS.Modules.Branding.Endpoints;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Branding.Endpoints;

/// <summary>Theme &amp; Branding tabs a Platform Admin switched off (ADR-145) keep their saved
/// values whatever a save request says.</summary>
public class BrandingControllerTests
{
    private readonly IBrandingService _service = Substitute.For<IBrandingService>();
    private readonly TenantContext _tenantContext = new();

    private static readonly BrandingResponse Current = new()
    {
        HospitalName = "Lakshmi Hospitals",
        AppTitle = "HMIS",
        Address = "1 Main Road",
        PhoneNumber = "0462-123456",
        FontFamily = "Inter",
        FontSizeScale = "md",
        IconSizeScale = "md",
        TokensLight = new Dictionary<string, string> { ["--primary"] = "38 92% 50%" },
        TokensDark = new Dictionary<string, string> { ["--primary"] = "38 92% 40%" },
    };

    private static UpdateBrandingRequest Incoming() => new()
    {
        HospitalName = "Renamed",
        AppTitle = "New title",
        Address = "Somewhere else",
        PhoneNumber = "000",
        FontFamily = "Roboto",
        FontSizeScale = "lg",
        IconSizeScale = "xl",
        TokensLight = new Dictionary<string, string> { ["--primary"] = "0 0% 0%" },
        TokensDark = new Dictionary<string, string> { ["--primary"] = "0 0% 10%" },
        LogoDisplay = new LogoDisplaySettings(),
        PrintSettings = new PrintSettings(),
    };

    public BrandingControllerTests()
    {
        _service.GetAsync(Arg.Any<CancellationToken>()).Returns(Current);
        _service.UpdateAsync(Arg.Any<UpdateBrandingRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Result<BrandingResponse>.Success(Current));
    }

    private BrandingController NewController(params string[] switchedOff)
    {
        var features = FeatureCatalog.AllWithSubFeatures.Where(key => !switchedOff.Contains(key)).ToList();
        _tenantContext.SetTenant(Guid.NewGuid(), "Host=localhost", enabledFeatures: features);
        return new BrandingController(_service, _tenantContext)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
    }

    private UpdateBrandingRequest Saved() =>
        (UpdateBrandingRequest)_service.ReceivedCalls().Single(call => call.GetMethodInfo().Name == nameof(IBrandingService.UpdateAsync)).GetArguments()[0]!;

    [Fact]
    public async Task Update_WithEveryTabOn_SavesTheRequestAsSent()
    {
        var request = Incoming();

        await NewController().Update(request, CancellationToken.None);

        Saved().Should().BeSameAs(request);
    }

    [Fact]
    public async Task Update_WithHospitalInfoAndTypographyOff_KeepsThoseValues()
    {
        await NewController("settings.branding.hospital-info", "settings.branding.typography").Update(Incoming(), CancellationToken.None);

        var saved = Saved();
        saved.HospitalName.Should().Be(Current.HospitalName);
        saved.AppTitle.Should().Be(Current.AppTitle);
        saved.Address.Should().Be(Current.Address);
        saved.PhoneNumber.Should().Be(Current.PhoneNumber);
        saved.FontFamily.Should().Be(Current.FontFamily);
        saved.IconSizeScale.Should().Be(Current.IconSizeScale);
        saved.TokensLight["--primary"].Should().Be("0 0% 0%", "the color tabs are still on");
    }

    [Fact]
    public async Task Update_WithLogoAndPrintOff_LeavesThoseSettingsUnchanged()
    {
        await NewController("settings.branding.logo", "settings.branding.print").Update(Incoming(), CancellationToken.None);

        Saved().LogoDisplay.Should().BeNull();
        Saved().PrintSettings.Should().BeNull();
    }

    [Fact]
    public async Task Update_WithOnlySomeColorTabsOff_StillSavesTheColors()
    {
        await NewController("settings.branding.buttons").Update(Incoming(), CancellationToken.None);

        Saved().TokensLight["--primary"].Should().Be("0 0% 0%");
    }

    [Fact]
    public async Task Update_WithEveryColorTabOff_KeepsTheColors()
    {
        await NewController(
            "settings.branding.core-colors",
            "settings.branding.top-bar",
            "settings.branding.left-nav",
            "settings.branding.section-headers",
            "settings.branding.buttons").Update(Incoming(), CancellationToken.None);

        Saved().TokensLight["--primary"].Should().Be("38 92% 50%");
        Saved().TokensDark["--primary"].Should().Be("38 92% 40%");
        Saved().HospitalName.Should().Be("Renamed");
    }
}
