using System.Net;
using System.Security.Claims;
using FluentAssertions;
using HMS.Shared.Infrastructure;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace HMS.UnitTests.Shared.Infrastructure;

public class RateLimitPartitionKeysTests
{
    private static DefaultHttpContext ContextWith(string ip, params Claim[] claims)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        if (claims.Length > 0)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        }

        return context;
    }

    [Fact]
    public void Caller_UsesUserId_WhenAuthenticatedHospitalUser()
    {
        var userId = Guid.NewGuid();
        var context = ContextWith("10.0.0.5", new Claim("UserId", userId.ToString()));

        RateLimitPartitionKeys.Caller(context).Should().Be($"user:{userId}");
    }

    [Fact]
    public void Caller_UsesPlatformUserId_WhenAuthenticatedPlatformAdmin()
    {
        var platformUserId = Guid.NewGuid();
        var context = ContextWith("10.0.0.5", new Claim("PlatformUserId", platformUserId.ToString()));

        RateLimitPartitionKeys.Caller(context).Should().Be($"platform:{platformUserId}");
    }

    [Fact]
    public void Caller_FallsBackToIp_WhenAnonymous()
    {
        var context = ContextWith("10.0.0.5");

        RateLimitPartitionKeys.Caller(context).Should().Be("ip:10.0.0.5");
    }

    [Fact]
    public void Caller_FallsBackToIp_WhenUserIdClaimMalformed()
    {
        var context = ContextWith("10.0.0.5", new Claim("UserId", "not-a-guid"));

        RateLimitPartitionKeys.Caller(context).Should().Be("ip:10.0.0.5");
    }

    [Fact]
    public void Caller_GivesDifferentUsersBehindSameIpSeparatePartitions()
    {
        var first = ContextWith("203.0.113.9", new Claim("UserId", Guid.NewGuid().ToString()));
        var second = ContextWith("203.0.113.9", new Claim("UserId", Guid.NewGuid().ToString()));

        RateLimitPartitionKeys.Caller(first).Should().NotBe(RateLimitPartitionKeys.Caller(second));
        RateLimitPartitionKeys.ClientIp(first).Should().Be(RateLimitPartitionKeys.ClientIp(second));
    }

    [Fact]
    public void ClientIp_IgnoresAuthentication()
    {
        var context = ContextWith("10.0.0.5", new Claim("UserId", Guid.NewGuid().ToString()));

        RateLimitPartitionKeys.ClientIp(context).Should().Be("ip:10.0.0.5");
    }
}
