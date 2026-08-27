using FluentAssertions;
using HMS.Modules.Notifications.Application;
using HMS.Modules.Notifications.Application.Abstractions;
using HMS.Modules.Notifications.Contracts;
using HMS.Modules.Notifications.Domain;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Notifications.Application;

public class NotificationPreferenceServiceTests
{
    private readonly INotificationPreferenceRepository _repository = Substitute.For<INotificationPreferenceRepository>();
    private readonly NotificationPreferenceService _sut;

    public NotificationPreferenceServiceTests()
    {
        _sut = new NotificationPreferenceService(_repository);
    }

    [Fact]
    public async Task GetMyPreferencesAsync_MapsEverySavedRow()
    {
        var userId = Guid.NewGuid();
        _repository.GetByUserAsync(userId, Arg.Any<CancellationToken>()).Returns(
        [
            NotificationPreference.Create(userId, "billing", null),
        ]);

        var result = await _sut.GetMyPreferencesAsync(userId, CancellationToken.None);

        result.Should().ContainSingle(p => p.Category == "billing");
    }

    [Fact]
    public async Task UpsertMyPreferenceAsync_WhenNoRowExists_CreatesOne()
    {
        var userId = Guid.NewGuid();
        _repository.GetByUserAndCategoryAsync(userId, "billing", Arg.Any<CancellationToken>()).Returns((NotificationPreference?)null);

        var request = new UpdateNotificationPreferenceRequest { Category = "billing", InAppEnabled = true, EmailEnabled = false, SmsEnabled = true };

        var result = await _sut.UpsertMyPreferenceAsync(userId, request, CancellationToken.None);

        result.EmailEnabled.Should().BeFalse();
        result.SmsEnabled.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<NotificationPreference>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpsertMyPreferenceAsync_WhenRowExists_UpdatesItInPlace()
    {
        var userId = Guid.NewGuid();
        var existing = NotificationPreference.Create(userId, "billing", null);
        _repository.GetByUserAndCategoryAsync(userId, "billing", Arg.Any<CancellationToken>()).Returns(existing);

        var request = new UpdateNotificationPreferenceRequest { Category = "billing", InAppEnabled = false, EmailEnabled = false, SmsEnabled = false };

        var result = await _sut.UpsertMyPreferenceAsync(userId, request, CancellationToken.None);

        result.Id.Should().Be(existing.Id);
        result.InAppEnabled.Should().BeFalse();
        await _repository.DidNotReceive().AddAsync(Arg.Any<NotificationPreference>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
