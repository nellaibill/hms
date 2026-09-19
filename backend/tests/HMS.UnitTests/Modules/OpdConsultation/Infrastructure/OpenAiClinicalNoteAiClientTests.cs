using FluentAssertions;
using HMS.Modules.OpdConsultation.Application;
using HMS.Modules.OpdConsultation.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HMS.UnitTests.Modules.OpdConsultation.Infrastructure;

public class OpenAiClinicalNoteAiClientTests
{
    [Fact]
    public async Task StructureAsync_WithoutConfiguredApiKey_ReturnsAiNotConfiguredWithoutThrowing()
    {
        var configuration = new ConfigurationBuilder().Build();
        using var httpClient = new HttpClient();
        var sut = new OpenAiClinicalNoteAiClient(httpClient, configuration, NullLogger<OpenAiClinicalNoteAiClient>.Instance);

        var act = () => sut.StructureAsync("Patient reports knee pain since two weeks.", CancellationToken.None);

        var result = await act.Should().NotThrowAsync();
        result.Subject.IsSuccess.Should().BeFalse();
        result.Subject.ErrorCode.Should().Be(OpdConsultationErrorCodes.AiNotConfigured);
    }
}
