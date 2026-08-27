using FluentAssertions;
using HMS.Modules.Notifications.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HMS.UnitTests.Modules.Notifications.Infrastructure;

public class HttpSmsSenderTests
{
    [Fact]
    public async Task SendAsync_WithoutConfiguredBaseUrl_NoOpsWithoutThrowing()
    {
        var configuration = new ConfigurationBuilder().Build();
        using var httpClient = new HttpClient();
        var sut = new HttpSmsSender(httpClient, configuration, NullLogger<HttpSmsSender>.Instance);

        var act = () => sut.SendAsync("+919876543210", "Body", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendAsync_WithBaseUrlButNoApiKey_NoOpsWithoutThrowing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Notifications:Sms:BaseUrl"] = "https://sms.example.com/send" })
            .Build();
        using var httpClient = new HttpClient();
        var sut = new HttpSmsSender(httpClient, configuration, NullLogger<HttpSmsSender>.Instance);

        var act = () => sut.SendAsync("+919876543210", "Body", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
