using FluentAssertions;
using HMS.Modules.ActivityLog.Application;
using HMS.Modules.ActivityLog.Application.Abstractions;
using HMS.Modules.ActivityLog.Contracts;
using HMS.Modules.ActivityLog.Domain;
using HMS.Shared.Kernel;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.ActivityLog.Application;

public class ActivityLogQueryServiceTests
{
    private readonly IActivityLogRepository _repository = Substitute.For<IActivityLogRepository>();
    private readonly ActivityLogQueryService _sut;

    public ActivityLogQueryServiceTests()
    {
        _sut = new ActivityLogQueryService(_repository);
    }

    private static ActivityLogEntry NewEntry(string? oldValues = null, string? newValues = null) =>
        ActivityLogEntry.Create(Guid.NewGuid(), Guid.NewGuid(), "Update", "Patients", "Patient", "p-1", "Updated", oldValues, newValues, "1.2.3.4", "agent", "corr", true);

    [Fact]
    public async Task GetPagedAsync_WithFromAfterTo_ReturnsInvalidDateRangeFailure()
    {
        var query = new ActivityLogListQuery { From = new DateTime(2026, 2, 1), To = new DateTime(2026, 1, 1) };

        var result = await _sut.GetPagedAsync(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ActivityLogErrorCodes.InvalidDateRange);
        await _repository.DidNotReceive().GetPagedAsync(Arg.Any<ActivityLogListQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPagedAsync_MapsEntriesAndKeepsPagingMetadata_WithoutExposingValues()
    {
        var entry = NewEntry("{\"a\":1}", "{\"a\":2}");
        var query = new ActivityLogListQuery { Page = 2, PageSize = 10 };
        _repository.GetPagedAsync(query, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ActivityLogEntry>([entry], 2, 10, 11));

        var result = await _sut.GetPagedAsync(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Page.Should().Be(2);
        result.Value.TotalCount.Should().Be(11);
        var item = result.Value.Items.Single();
        item.Id.Should().Be(entry.Id);
        item.Action.Should().Be("Update");
        item.Module.Should().Be("Patients");
        item.EntityId.Should().Be("p-1");
        item.Should().NotBeOfType<ActivityLogDetailResponse>();
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ActivityLogEntry?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ActivityLogErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDetailsWithParsedOldAndNewValues()
    {
        var entry = NewEntry("{\"name\":\"before\"}", "{\"name\":\"after\"}");
        _repository.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);

        var result = await _sut.GetByIdAsync(entry.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OldValues!.Value.GetProperty("name").GetString().Should().Be("before");
        result.Value.NewValues!.Value.GetProperty("name").GetString().Should().Be("after");
        result.Value.IpAddress.Should().Be("1.2.3.4");
        result.Value.UserAgent.Should().Be("agent");
        result.Value.CorrelationId.Should().Be("corr");
    }
}
