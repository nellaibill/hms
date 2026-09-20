using FluentAssertions;
using HMS.Modules.ActivityLog.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.ActivityLog.Domain;

public class ActivityLogEntryTests
{
    [Fact]
    public void Create_SetsIdTimestampAndSuppliedFields()
    {
        var before = DateTime.UtcNow;

        var entry = ActivityLogEntry.Create(null, null, " Create ", " Patients ", "Patient", "id-1", "desc", null, null, null, null, null, true);

        entry.Id.Should().NotBeEmpty();
        entry.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
        entry.Action.Should().Be("Create");
        entry.Module.Should().Be("Patients");
        entry.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Patients")]
    [InlineData("Create", " ")]
    public void Create_WithBlankActionOrModule_Throws(string action, string module)
    {
        var act = () => ActivityLogEntry.Create(null, null, action, module, null, null, null, null, null, null, null, null, true);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Create_TruncatesOverlongFieldsToTheirColumnLimits()
    {
        var entry = ActivityLogEntry.Create(null, null, "Create", "Patients", new string('e', 500), new string('i', 500), new string('d', 5000), null, null, new string('p', 200), new string('u', 2000), new string('c', 500), true);

        entry.EntityType!.Length.Should().Be(100);
        entry.EntityId!.Length.Should().Be(100);
        entry.Description!.Length.Should().Be(1000);
        entry.IpAddress!.Length.Should().Be(45);
        entry.UserAgent!.Length.Should().Be(512);
        entry.CorrelationId!.Length.Should().Be(100);
    }

    [Fact]
    public void Entry_ExposesNoPublicMutators()
    {
        var mutators = typeof(ActivityLogEntry).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName);
        var publicSetters = typeof(ActivityLogEntry).GetProperties().Where(p => p.SetMethod is { IsPublic: true });

        mutators.Should().BeEmpty();
        publicSetters.Should().BeEmpty();
    }
}
