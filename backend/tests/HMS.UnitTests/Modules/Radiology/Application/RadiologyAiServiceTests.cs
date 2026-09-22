using FluentAssertions;
using HMS.Modules.Documents.Application;
using HMS.Modules.Radiology.Application;
using HMS.Modules.Radiology.Application.Abstractions;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Radiology.Application;

public class RadiologyAiServiceTests
{
    private static readonly DocumentActor Actor = new(Guid.NewGuid(), "doctor");

    private readonly IDocumentService _documents = Substitute.For<IDocumentService>();
    private readonly IXrayImageAnalyzer _analyzer = Substitute.For<IXrayImageAnalyzer>();
    private readonly RadiologyAiService _sut;

    public RadiologyAiServiceTests()
    {
        _sut = new RadiologyAiService(_documents, _analyzer, NullLogger<RadiologyAiService>.Instance);
    }

    private void GivenDocument(byte[] bytes, string contentType)
    {
        _documents
            .GetContentAsync(Arg.Any<Guid>(), Arg.Any<DocumentActor>(), Arg.Any<CancellationToken>())
            .Returns(_ => Result<DocumentContent>.Success(new DocumentContent(new MemoryStream(bytes), contentType, "xray.png")));
    }

    [Fact]
    public async Task AnalyzeDocument_PassesDocumentFailureThrough_WithoutCallingTheModel()
    {
        _documents
            .GetContentAsync(Arg.Any<Guid>(), Arg.Any<DocumentActor>(), Arg.Any<CancellationToken>())
            .Returns(Result<DocumentContent>.Failure("DOCUMENTS.DOCUMENT_NOT_FOUND", "not found"));

        var result = await _sut.AnalyzeDocumentAsync(Guid.NewGuid(), Actor, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(RadiologyAiErrorCodes.DocumentNotFound);
        await _analyzer.DidNotReceiveWithAnyArgs().AnalyzeAsync(default!, default!, default);
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("application/dicom")]
    [InlineData("text/plain")]
    public async Task AnalyzeDocument_RejectsNonImageContentTypes(string contentType)
    {
        GivenDocument([1, 2, 3], contentType);

        var result = await _sut.AnalyzeDocumentAsync(Guid.NewGuid(), Actor, CancellationToken.None);

        result.ErrorCode.Should().Be(RadiologyAiErrorCodes.UnsupportedImage);
        await _analyzer.DidNotReceiveWithAnyArgs().AnalyzeAsync(default!, default!, default);
    }

    [Fact]
    public async Task AnalyzeDocument_RejectsImagesOverTheSizeCap()
    {
        GivenDocument(new byte[RadiologyAiService.MaxImageBytes + 1], "image/png");

        var result = await _sut.AnalyzeDocumentAsync(Guid.NewGuid(), Actor, CancellationToken.None);

        result.ErrorCode.Should().Be(RadiologyAiErrorCodes.ImageTooLarge);
        await _analyzer.DidNotReceiveWithAnyArgs().AnalyzeAsync(default!, default!, default);
    }

    [Fact]
    public async Task AnalyzeDocument_SendsOnlyImageBytesAndReturnsTheDraftWithDisclaimer()
    {
        var documentId = Guid.NewGuid();
        byte[] bytes = [9, 8, 7];
        GivenDocument(bytes, "image/png");
        _analyzer
            .AnalyzeAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<XrayImageAnalysis>.Success(new XrayImageAnalysis("ANATOMICAL REGION:\nWrist", "google/medgemma-4b-it")));

        var result = await _sut.AnalyzeDocumentAsync(documentId, Actor, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DocumentId.Should().Be(documentId);
        result.Value.Analysis.Should().StartWith("ANATOMICAL REGION:");
        result.Value.Model.Should().Be("google/medgemma-4b-it");
        result.Value.Disclaimer.Should().Be(XrayAnalysisPrompt.Disclaimer);
        await _analyzer.Received(1).AnalyzeAsync(Arg.Is<byte[]>(b => b.SequenceEqual(bytes)), "image/png", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyzeDocument_PassesAnalyzerFailureThrough()
    {
        GivenDocument([1], "image/jpeg");
        _analyzer
            .AnalyzeAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<XrayImageAnalysis>.Failure(RadiologyAiErrorCodes.NotConfigured, "not configured"));

        var result = await _sut.AnalyzeDocumentAsync(Guid.NewGuid(), Actor, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(RadiologyAiErrorCodes.NotConfigured);
    }
}
