using FluentAssertions;
using HMS.Modules.Branding.Application;
using HMS.Modules.Branding.Application.Abstractions;
using HMS.Modules.Branding.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace HMS.UnitTests.Modules.Branding.Application;

public class BrandingServiceTests
{
    private readonly IBrandingRepository _repository = Substitute.For<IBrandingRepository>();
    private readonly IBrandingLogoStorage _logoStorage = Substitute.For<IBrandingLogoStorage>();
    private readonly BrandingService _sut;

    public BrandingServiceTests()
    {
        _sut = new BrandingService(_repository, _logoStorage, NullLogger<BrandingService>.Instance);

        _repository.GetAsync(Arg.Any<CancellationToken>())
            .Returns(BrandingSettings.CreateDefault("Hospital", "App", "Inter", "md", "md", "{}", "{}"));
        _logoStorage.SaveAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("uploads/branding/logo/fake.png");
    }

    [Fact]
    public async Task UploadLogoAsync_DownscalesAnOversizedRasterImage()
    {
        using var original = new Image<Rgba32>(1000, 800);
        using var originalBytes = new MemoryStream();
        original.SaveAsPng(originalBytes);
        originalBytes.Position = 0;

        var storedBytes = await CaptureStoredBytesAsync(originalBytes, originalBytes.Length);

        using var stored = Image.Load(storedBytes);
        stored.Width.Should().BeLessOrEqualTo(512);
        stored.Height.Should().BeLessOrEqualTo(512);
        // Longer edge (width, 1000x800) should land exactly on the cap; aspect ratio preserved.
        stored.Width.Should().Be(512);
        stored.Height.Should().Be(410);
    }

    [Fact]
    public async Task UploadLogoAsync_StoresAnAlreadySmallRasterImageUnchanged()
    {
        using var original = new Image<Rgba32>(100, 60);
        using var originalBytes = new MemoryStream();
        original.SaveAsPng(originalBytes);
        var originalArray = originalBytes.ToArray();
        originalBytes.Position = 0;

        var storedBytes = await CaptureStoredBytesAsync(originalBytes, originalArray.Length);

        storedBytes.Should().Equal(originalArray);
    }

    private async Task<byte[]> CaptureStoredBytesAsync(Stream content, long length)
    {
        byte[]? storedBytes = null;
        _logoStorage.SaveAsync(Arg.Any<string>(), Arg.Do<Stream>(s => storedBytes = ReadAll(s)), Arg.Any<CancellationToken>())
            .Returns("uploads/branding/logo/fake.png");

        var result = await _sut.UploadLogoAsync(content, "logo.png", length, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        storedBytes.Should().NotBeNull();
        return storedBytes!;
    }

    [Fact]
    public async Task UploadLogoAsync_AcceptsACleanSvg()
    {
        var result = await UploadSvgAsync("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 10 10\"><circle cx=\"5\" cy=\"5\" r=\"4\" /></svg>");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UploadLogoAsync_RejectsAnSvgContainingAScriptElement()
    {
        var result = await UploadSvgAsync("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UploadLogoAsync_RejectsAnSvgContainingAnEventHandlerAttribute()
    {
        // Never contained the literal "<script" the old string-matching check looked for —
        // this is exactly the bypass class ADR-075 closed.
        var result = await UploadSvgAsync("<svg xmlns=\"http://www.w3.org/2000/svg\" onload=\"alert(1)\"><rect width=\"1\" height=\"1\" /></svg>");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UploadLogoAsync_RejectsAnSvgContainingAForeignObject()
    {
        var result = await UploadSvgAsync("<svg xmlns=\"http://www.w3.org/2000/svg\"><foreignObject><body xmlns=\"http://www.w3.org/1999/xhtml\">html</body></foreignObject></svg>");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UploadLogoAsync_RejectsAnSvgContainingAJavascriptUri()
    {
        var result = await UploadSvgAsync("<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\"><a xlink:href=\"javascript:alert(1)\"><rect width=\"1\" height=\"1\" /></a></svg>");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UploadLogoAsync_RejectsMalformedSvgMarkup()
    {
        var result = await UploadSvgAsync("<svg xmlns=\"http://www.w3.org/2000/svg\"><rect></svg>");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UploadLogoAsync_RejectsAnSvgWithADoctype()
    {
        var result = await UploadSvgAsync("<?xml version=\"1.0\"?><!DOCTYPE svg [<!ENTITY x \"x\">]><svg xmlns=\"http://www.w3.org/2000/svg\"><rect width=\"1\" height=\"1\" /></svg>");

        result.IsSuccess.Should().BeFalse();
    }

    private async Task<HMS.Shared.Kernel.Result<HMS.Modules.Branding.Contracts.BrandingResponse>> UploadSvgAsync(string svgMarkup)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(svgMarkup);
        using var content = new MemoryStream(bytes);
        return await _sut.UploadLogoAsync(content, "logo.svg", bytes.Length, actorId: null, CancellationToken.None);
    }

    private static byte[] ReadAll(Stream stream)
    {
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }
}
