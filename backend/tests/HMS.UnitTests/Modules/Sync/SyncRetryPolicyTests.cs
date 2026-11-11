using FluentAssertions;
using HMS.Modules.Sync.Application;
using Xunit;

namespace HMS.UnitTests.Modules.Sync;

public class SyncRetryPolicyTests
{
    private static readonly int[] Schedule = [1, 2, 5, 10, 30];

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 5)]
    [InlineData(3, 10)]
    [InlineData(4, 30)]
    public void OnTransientFailure_FollowsTheSchedule_WithNoJitterAtTheMidpoint(int retriesSoFar, int expectedMinutes)
    {
        var policy = new SyncRetryPolicy(Schedule, maxRetryCount: 5, random: () => 0.5);

        var decision = policy.OnTransientFailure(retriesSoFar);

        decision.Exhausted.Should().BeFalse();
        decision.RetryCount.Should().Be(retriesSoFar + 1);
        decision.Delay.Should().Be(TimeSpan.FromMinutes(expectedMinutes));
    }

    [Fact]
    public void OnTransientFailure_RepeatsTheLastEntryBeyondTheSchedule()
    {
        var policy = new SyncRetryPolicy(Schedule, maxRetryCount: 10, random: () => 0.5);

        policy.OnTransientFailure(7).Delay.Should().Be(TimeSpan.FromMinutes(30));
    }

    [Theory]
    [InlineData(0.0, 0.9)]
    [InlineData(1.0, 1.1)]
    public void OnTransientFailure_JitterStaysWithinTenPercent(double random, double factor)
    {
        var policy = new SyncRetryPolicy(Schedule, maxRetryCount: 5, random: () => random);

        policy.OnTransientFailure(2).Delay!.Value.TotalMinutes.Should().BeApproximately(5 * factor, 0.0001);
    }

    [Fact]
    public void OnTransientFailure_NeedsAttentionOnceMaxRetryCountIsExceeded()
    {
        var policy = new SyncRetryPolicy(Schedule, maxRetryCount: 5);

        policy.OnTransientFailure(4).Exhausted.Should().BeFalse("the 5th retry is still allowed");

        var decision = policy.OnTransientFailure(5);
        decision.Exhausted.Should().BeTrue();
        decision.Delay.Should().BeNull();
        decision.RetryCount.Should().Be(6);
    }

    [Fact]
    public void OnTransientFailure_WithZeroRetriesAllowed_NeedsAttentionImmediately()
    {
        new SyncRetryPolicy(Schedule, maxRetryCount: 0).OnTransientFailure(0).Exhausted.Should().BeTrue();
    }
}
