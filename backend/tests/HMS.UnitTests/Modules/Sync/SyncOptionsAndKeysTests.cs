using FluentAssertions;
using HMS.Modules.Sync;
using HMS.Modules.Sync.Application;
using HMS.Modules.Sync.Contracts;
using HMS.Modules.Sync.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace HMS.UnitTests.Modules.Sync;

public class SyncOptionsValidatorTests
{
    private readonly SyncOptionsValidator _validator = new();

    [Fact]
    public void Defaults_AreValid_AndSyncIsOff()
    {
        var options = new SyncOptions();

        _validator.Validate(null, options).Succeeded.Should().BeTrue();
        options.Role.Should().Be(SyncRole.None);
        options.EffectiveRetrySchedule.Should().Equal(1, 2, 5, 10, 30);
    }

    [Theory]
    [InlineData("https://cloud.example.com", true)]
    [InlineData("http://localhost:5000", true)]
    [InlineData("http://127.0.0.1:5000", true)]
    [InlineData("http://cloud.example.com", false)]
    [InlineData("", false)]
    [InlineData("not a url", false)]
    public void Edge_RequiresAnHttpsCloudUrl_ExceptOnLocalhost(string baseUrl, bool valid)
    {
        var options = new SyncOptions { Role = SyncRole.Edge, Cloud = new SyncCloudOptions { BaseUrl = baseUrl } };

        _validator.Validate(null, options).Succeeded.Should().Be(valid);
    }

    [Fact]
    public void Cloud_DoesNotNeedACloudUrl()
    {
        _validator.Validate(null, new SyncOptions { Role = SyncRole.Cloud }).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void LeaseShorterThanTheHttpTimeoutPlusMargin_IsRejected()
    {
        var result = _validator.Validate(null, new SyncOptions { HttpTimeoutInSeconds = 60, LeaseSeconds = 60 });

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("LeaseSeconds");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(60)]
    public void IntervalOutOfRange_IsRejected(int minutes)
    {
        _validator.Validate(null, new SyncOptions { IntervalInMinutes = minutes }).Failed.Should().BeTrue();
    }

    [Fact]
    public void MaxEventsPerStreamAboveBatchSize_IsRejected()
    {
        _validator.Validate(null, new SyncOptions { BatchSize = 10, MaxEventsPerStream = 11 }).Failed.Should().BeTrue();
    }

    [Fact]
    public void ApiKeys_BoundFromConfiguration_AreFoundForALowercaseHospitalCode()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Sync:Cloud:ApiKeys:LHS"] = "the-key" })
            .Build();
        var options = new SyncOptions();
        configuration.GetSection("Sync").Bind(options);

        options.FindApiKey("lhs").Should().Be("the-key");
    }

    [Fact]
    public void ApiKeys_AreLookedUpCaseInsensitively_AndBlankKeysCountAsMissing()
    {
        var options = new SyncOptions();
        options.Cloud.ApiKeys["LHS"] = " key ";
        options.Cloud.ApiKeys["XYZ"] = " ";

        options.FindApiKey("lhs").Should().Be("key");
        options.FindApiKey("XYZ").Should().BeNull();
        options.FindApiKey("other").Should().BeNull();
    }
}

public class SyncApiKeysTests
{
    [Fact]
    public void Generate_ProducesWellFormedUniqueKeys()
    {
        var first = SyncApiKeys.Generate();
        var second = SyncApiKeys.Generate();

        SyncApiKeys.IsWellFormed(first).Should().BeTrue();
        first.Should().StartWith(SyncApiKeys.Prefix).And.NotBe(second);
    }

    [Fact]
    public void Hash_IsStableLowercaseSha256Hex_AndDiffersPerKey()
    {
        var key = SyncApiKeys.Generate();

        SyncApiKeys.Hash(key).Should().Be(SyncApiKeys.Hash(key)).And.MatchRegex("^[0-9a-f]{64}$");
        SyncApiKeys.Hash(key).Should().NotBe(SyncApiKeys.Hash(SyncApiKeys.Generate()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("hmssync_short")]
    [InlineData("bearer_AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("hmssync_AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA!")]
    public void IsWellFormed_RejectsAnythingElse(string? key)
    {
        SyncApiKeys.IsWellFormed(key).Should().BeFalse();
    }

    [Fact]
    public void DisplayPrefix_RevealsOnlyTheStartOfTheKey()
    {
        var key = SyncApiKeys.Generate();

        SyncApiKeys.DisplayPrefix(key).Should().HaveLength(14);
        key.Should().StartWith(SyncApiKeys.DisplayPrefix(key));
    }

    [Theory]
    [InlineData("HmsSync abc", true)]
    [InlineData("hmssync abc", true)]
    [InlineData("Bearer abc", false)]
    [InlineData("HmsSyncabc", false)]
    [InlineData(null, false)]
    public void IsSyncAuthorizationHeader_MatchesOnlyTheSyncScheme(string? header, bool expected)
    {
        SyncAuthenticationDefaults.IsSyncAuthorizationHeader(header).Should().Be(expected);
    }
}

public class HangfireDashboardAccessTests
{
    private readonly HangfireDashboardAccess _access = new(
        new EphemeralDataProtectionProvider(),
        new MemoryCache(new MemoryCacheOptions()),
        Options.Create(new SyncHangfireOptions()));

    private static string TokenFrom(string url) =>
        Uri.UnescapeDataString(url[(url.IndexOf("access=", StringComparison.Ordinal) + "access=".Length)..]);

    [Fact]
    public void Handoff_IsRedeemedOnce_ForAValidSession()
    {
        var (url, _) = _access.CreateHandoff(Guid.NewGuid(), Guid.NewGuid());
        url.Should().StartWith("/hangfire?access=");

        _access.TryRedeemHandoff(TokenFrom(url), out var session, out var expires).Should().BeTrue();
        _access.IsValidSession(session).Should().BeTrue();
        expires.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(10));

        _access.TryRedeemHandoff(TokenFrom(url), out _, out _).Should().BeFalse("a dashboard link works only once");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-token")]
    public void ForgedHandoff_IsRefused(string token)
    {
        _access.TryRedeemHandoff(token, out _, out _).Should().BeFalse();
    }

    [Fact]
    public void Session_RejectsMissingOrTamperedCookies_AndLinkTokensUsedAsCookies()
    {
        var (url, _) = _access.CreateHandoff(Guid.NewGuid(), Guid.NewGuid());

        _access.IsValidSession(null).Should().BeFalse();
        _access.IsValidSession("garbage").Should().BeFalse();
        _access.IsValidSession(TokenFrom(url)).Should().BeFalse("a link token is not a session");
    }
}

public class SyncCommandLineTests
{
    [Fact]
    public void PositionalArguments_StopAtTheFirstSwitch_SoASwitchValueIsNeverTakenAsAKeyPrefix()
    {
        string[] args = ["sync-key", "revoke", "LHS", "--environment", "Production", "--urls", "http://x"];

        SyncCommandLine.PositionalArguments(args, 0).Should().Equal("revoke", "LHS");
    }

    [Fact]
    public void PositionalArguments_KeepAnExplicitPrefix()
    {
        string[] args = ["--environment=Production", "sync-key", "revoke", "LHS", "hmssync_abcdef", "--x", "y"];

        SyncCommandLine.PositionalArguments(args, 1).Should().Equal("revoke", "LHS", "hmssync_abcdef");
    }
}
