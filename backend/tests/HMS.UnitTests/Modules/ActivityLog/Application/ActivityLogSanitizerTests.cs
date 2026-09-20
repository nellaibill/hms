using System.Text.Json;
using FluentAssertions;
using HMS.Modules.ActivityLog.Application;
using Xunit;

namespace HMS.UnitTests.Modules.ActivityLog.Application;

public class ActivityLogSanitizerTests
{
    private enum Color { Red }

    [Fact]
    public void Sanitize_WithNull_ReturnsNull()
    {
        ActivityLogSanitizer.Sanitize(null).Should().BeNull();
    }

    [Theory]
    [InlineData("Password")]
    [InlineData("NewPassword")]
    [InlineData("PasswordHash")]
    [InlineData("AccessToken")]
    [InlineData("refreshToken")]
    [InlineData("ClientSecret")]
    [InlineData("ApiKey")]
    [InlineData("Authorization")]
    public void Sanitize_RedactsSensitivePropertyNames(string propertyName)
    {
        var value = new Dictionary<string, object?> { [propertyName] = "super-secret", ["Name"] = "Jane" };

        var json = ActivityLogSanitizer.Sanitize(value);

        json.Should().NotContain("super-secret");
        var root = JsonDocument.Parse(json!).RootElement;
        root.GetProperty(propertyName).GetString().Should().Be(ActivityLogSanitizer.RedactedMarker);
        root.GetProperty("Name").GetString().Should().Be("Jane");
    }

    [Fact]
    public void Sanitize_RedactsNestedObjectsAndArrays()
    {
        var value = new
        {
            User = new { Username = "jane", Password = "p@ss" },
            Items = new[] { new { Token = "abc", Qty = 2 } },
        };

        var json = ActivityLogSanitizer.Sanitize(value);

        json.Should().NotContain("p@ss").And.NotContain("abc");
        var root = JsonDocument.Parse(json!).RootElement;
        root.GetProperty("User").GetProperty("Username").GetString().Should().Be("jane");
        root.GetProperty("Items")[0].GetProperty("Qty").GetInt32().Should().Be(2);
    }

    [Fact]
    public void Sanitize_SerializesEnumsAsStrings()
    {
        var json = ActivityLogSanitizer.Sanitize(new { Color = Color.Red });

        JsonDocument.Parse(json!).RootElement.GetProperty("Color").GetString().Should().Be("Red");
    }

    [Fact]
    public void Sanitize_WithOversizedPayload_StoresATruncationMarkerInstead()
    {
        var json = ActivityLogSanitizer.Sanitize(new { Notes = new string('x', 100_000) });

        json.Should().Be("{\"_truncated\":true}");
    }
}
