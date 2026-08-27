using FluentAssertions;
using HMS.Modules.Notifications.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HMS.UnitTests.Modules.Notifications.Infrastructure;

public class SmtpEmailSenderTests
{
    [Fact]
    public async Task SendAsync_WithoutConfiguredHost_NoOpsWithoutThrowing()
    {
        var configuration = new ConfigurationBuilder().Build();
        var sut = new SmtpEmailSender(configuration, NullLogger<SmtpEmailSender>.Instance);

        var act = () => sut.SendAsync("patient@example.com", "Subject", "Body", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendAsync_WithHostButNoFromAddress_NoOpsWithoutThrowing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Notifications:Smtp:Host"] = "smtp.example.com" })
            .Build();
        var sut = new SmtpEmailSender(configuration, NullLogger<SmtpEmailSender>.Instance);

        var act = () => sut.SendAsync("patient@example.com", "Subject", "Body", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
