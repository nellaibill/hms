using System.Text.Json;
using FluentAssertions;
using HMS.Modules.OpdConsultation.Application;
using HMS.Modules.OpdConsultation.Infrastructure;
using HMS.Shared.Infrastructure.Ai;
using HMS.Shared.Kernel;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.OpdConsultation.Infrastructure;

public class ExtractorBackedClinicalNoteAiClientTests
{
    private readonly IAiStructuredExtractor _extractor = Substitute.For<IAiStructuredExtractor>();
    private readonly ExtractorBackedClinicalNoteAiClient _sut;

    public ExtractorBackedClinicalNoteAiClientTests()
    {
        _sut = new ExtractorBackedClinicalNoteAiClient(_extractor);
    }

    [Fact]
    public async Task StructureAsync_MapsTheExtractedJsonOntoTheNoteFields()
    {
        AiStructuredExtractionRequest? sent = null;
        _extractor.ExtractAsync(Arg.Do<AiStructuredExtractionRequest>(r => sent = r), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(JsonDocument.Parse("""{"presentingComplaints":"Knee pain","planOfManagement":"Rest, X-ray"}""").RootElement.Clone()));

        var result = await _sut.StructureAsync("Patient reports knee pain.", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PresentingComplaints.Should().Be("Knee pain");
        result.Value.PlanOfManagement.Should().Be("Rest, X-ray");
        result.Value.ClinicalHistory.Should().BeNull();
        sent!.UserContent.Should().Be("Patient reports knee pain.");
        sent.ToolName.Should().Be("structure_note");
    }

    [Fact]
    public async Task StructureAsync_WhenTheProviderIsNotConfigured_ReturnsTheOpdAiNotConfiguredCode()
    {
        _extractor.ExtractAsync(Arg.Any<AiStructuredExtractionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Failure(AiErrorCodes.NotConfigured, "not configured"));

        var result = await _sut.StructureAsync("x", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(OpdConsultationErrorCodes.AiNotConfigured);
    }

    [Fact]
    public async Task StructureAsync_WhenTheProviderCallFails_ReturnsTheOpdAiRequestFailedCode()
    {
        _extractor.ExtractAsync(Arg.Any<AiStructuredExtractionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Failure(AiErrorCodes.RequestFailed, "boom"));

        var result = await _sut.StructureAsync("x", CancellationToken.None);

        result.ErrorCode.Should().Be(OpdConsultationErrorCodes.AiRequestFailed);
    }
}
