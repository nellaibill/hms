using FluentAssertions;
using HMS.Modules.ActivityLog.Application;
using HMS.Modules.ActivityLog.Application.Abstractions;
using HMS.Modules.ActivityLog.Contracts;
using HMS.Modules.ActivityLog.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace HMS.UnitTests.Modules.ActivityLog.Application;

public class ActivityLogServiceTests
{
    private readonly IActivityLogRepository _repository = Substitute.For<IActivityLogRepository>();
    private readonly IActivityLogContextProvider _contextProvider = Substitute.For<IActivityLogContextProvider>();
    private readonly ActivityLogService _sut;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _contextUserId = Guid.NewGuid();

    public ActivityLogServiceTests()
    {
        _sut = new ActivityLogService(_repository, _contextProvider, NullLogger<ActivityLogService>.Instance);

        _contextProvider.GetCurrent().Returns(new ActivityLogContext(_tenantId, _contextUserId, "10.0.0.5", "UnitTest/1.0", "corr-123"));
    }

    private static ActivityLogRequest NewRequest() => new()
    {
        Action = ActivityLogActions.Update,
        Module = ActivityLogModules.Patients,
        EntityType = "Patient",
        EntityId = "abc",
        Description = "Updated patient",
    };

    [Fact]
    public async Task LogAsync_FillsTenantUserIpUserAgentAndCorrelationIdFromContext()
    {
        ActivityLogEntry? saved = null;
        await _repository.AddAsync(Arg.Do<ActivityLogEntry>(e => saved = e), Arg.Any<CancellationToken>());

        await _sut.LogAsync(NewRequest());

        saved.Should().NotBeNull();
        saved!.TenantId.Should().Be(_tenantId);
        saved.UserId.Should().Be(_contextUserId);
        saved.IpAddress.Should().Be("10.0.0.5");
        saved.UserAgent.Should().Be("UnitTest/1.0");
        saved.CorrelationId.Should().Be("corr-123");
        saved.Action.Should().Be(ActivityLogActions.Update);
        saved.Module.Should().Be(ActivityLogModules.Patients);
        saved.EntityType.Should().Be("Patient");
        saved.EntityId.Should().Be("abc");
        saved.IsSuccess.Should().BeTrue();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LogAsync_WithExplicitUserId_UsesItInsteadOfTheContextUser()
    {
        var explicitUser = Guid.NewGuid();
        ActivityLogEntry? saved = null;
        await _repository.AddAsync(Arg.Do<ActivityLogEntry>(e => saved = e), Arg.Any<CancellationToken>());

        await _sut.LogAsync(NewRequest() with { UserId = explicitUser });

        saved!.UserId.Should().Be(explicitUser);
    }

    [Fact]
    public async Task LogAsync_ScrubsSecretsFromOldAndNewValues()
    {
        ActivityLogEntry? saved = null;
        await _repository.AddAsync(Arg.Do<ActivityLogEntry>(e => saved = e), Arg.Any<CancellationToken>());

        await _sut.LogAsync(NewRequest() with
        {
            OldValues = new { Name = "Old", Password = "old-secret" },
            NewValues = new { Name = "New", AccessToken = "new-secret" },
        });

        saved!.OldValues.Should().Contain("Old").And.NotContain("old-secret");
        saved.NewValues.Should().Contain("New").And.NotContain("new-secret");
    }

    [Fact]
    public async Task LogAsync_WhenPersistingFails_DoesNotThrow()
    {
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("db down"));

        var act = () => _sut.LogAsync(NewRequest());

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogAsync_WhenContextCannotBeResolved_DoesNotThrow()
    {
        _contextProvider.GetCurrent().Throws(new InvalidOperationException("no tenant"));

        var act = () => _sut.LogAsync(NewRequest());

        await act.Should().NotThrowAsync();
        await _repository.DidNotReceive().AddAsync(Arg.Any<ActivityLogEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LogAsync_WithBlankAction_DoesNotThrowAndWritesNothing()
    {
        var act = () => _sut.LogAsync(NewRequest() with { Action = " " });

        await act.Should().NotThrowAsync();
        await _repository.DidNotReceive().AddAsync(Arg.Any<ActivityLogEntry>(), Arg.Any<CancellationToken>());
    }
}
