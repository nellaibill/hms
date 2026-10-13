using FluentAssertions;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace HMS.UnitTests.Shared.Infrastructure;

public class QueryStringGuardFilterTests
{
    private static ActionExecutingContext ContextFor(string queryString, params object[] endpointMetadata)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString(queryString);

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor { EndpointMetadata = endpointMetadata });

        return new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), controller: new object());
    }

    private static ApiErrorResponse RejectionOf(ActionExecutingContext context)
    {
        var result = context.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = result.Value.Should().BeOfType<ApiErrorResponse>().Subject;
        error.ErrorCode.Should().Be("VALIDATION.FAILED");
        return error;
    }

    [Theory]
    [InlineData("?search=%00")]
    [InlineData("?search=a%00b")]
    [InlineData("?page=1&Name=%00&pageSize=20")]
    public void RejectsAValueContainingANullCharacter(string queryString)
    {
        var context = ContextFor(queryString);

        new QueryStringGuardFilter().OnActionExecuting(context);

        RejectionOf(context).ValidationErrors.Should().ContainSingle()
            .Which.Message.Should().Be("Contains a character that isn't allowed.");
    }

    [Fact]
    public void RejectsAValueLongerThanTheDefaultLimit_NamingTheParameter()
    {
        var context = ContextFor("?page=1&search=" + new string('x', QueryStringGuardFilter.DefaultMaxValueLength + 1));

        new QueryStringGuardFilter().OnActionExecuting(context);

        var item = RejectionOf(context).ValidationErrors.Should().ContainSingle().Subject;
        item.Field.Should().Be("search");
        item.Message.Should().Be($"Must be {QueryStringGuardFilter.DefaultMaxValueLength} characters or fewer.");
    }

    [Fact]
    public void AllowsAValueExactlyAtTheLimit()
    {
        var context = ContextFor("?search=" + new string('x', QueryStringGuardFilter.DefaultMaxValueLength));

        new QueryStringGuardFilter().OnActionExecuting(context);

        context.Result.Should().BeNull();
    }

    [Fact]
    public void MeasuresTheDecodedValue_NotItsPercentEncoding()
    {
        // 60 Tamil letters are 60 characters, though they take 540 characters to percent-encode.
        var context = ContextFor("?search=" + Uri.EscapeDataString(new string('த', 60)));

        new QueryStringGuardFilter().OnActionExecuting(context);

        context.Result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("?page=1&pageSize=20&sort=-createdAt")]
    [InlineData("?search=O%27Brien%20%25_%5C&departmentId=01a0d169-3218-7ded-96d2-14abecf7ff6f&from=2026-10-01T00%3A00%3A00.000Z")]
    public void LetsOrdinaryQueriesThrough(string queryString)
    {
        var context = ContextFor(queryString);

        new QueryStringGuardFilter().OnActionExecuting(context);

        context.Result.Should().BeNull();
    }

    [Fact]
    public void ChecksEveryValueOfARepeatedParameter()
    {
        var context = ContextFor("?status=Paid&status=" + new string('x', QueryStringGuardFilter.DefaultMaxValueLength + 1));

        new QueryStringGuardFilter().OnActionExecuting(context);

        RejectionOf(context).ValidationErrors.Should().ContainSingle().Which.Field.Should().Be("status");
    }

    [Fact]
    public void ReportsEachOffendingParameterOnce()
    {
        var tooLong = new string('x', QueryStringGuardFilter.DefaultMaxValueLength + 1);
        var context = ContextFor($"?search={tooLong}&Name=%00&page=1");

        new QueryStringGuardFilter().OnActionExecuting(context);

        RejectionOf(context).ValidationErrors.Should().BeEquivalentTo(new[]
        {
            new ValidationErrorItem { Field = "search", Message = $"Must be {QueryStringGuardFilter.DefaultMaxValueLength} characters or fewer." },
            new ValidationErrorItem { Field = "Name", Message = "Contains a character that isn't allowed." },
        });
    }

    [Fact]
    public void AnActionCanRaiseTheLimit()
    {
        var context = ContextFor("?q=" + new string('x', 500), new QueryValueMaxLengthAttribute(500));

        new QueryStringGuardFilter().OnActionExecuting(context);

        context.Result.Should().BeNull();
    }

    [Fact]
    public void TheRaisedLimitIsStillEnforced_AndTheActionLevelValueWinsOverTheControllerLevelOne()
    {
        var context = ContextFor("?q=" + new string('x', 501), new QueryValueMaxLengthAttribute(50), new QueryValueMaxLengthAttribute(500));

        new QueryStringGuardFilter().OnActionExecuting(context);

        RejectionOf(context).ValidationErrors.Should().ContainSingle().Which.Message.Should().Be("Must be 500 characters or fewer.");
    }

    [Fact]
    public void DoesNotEchoAParameterNameThatCouldNotBeARealOne()
    {
        var context = ContextFor("?" + Uri.EscapeDataString("<script>") + "=%00");

        new QueryStringGuardFilter().OnActionExecuting(context);

        RejectionOf(context).ValidationErrors.Should().ContainSingle().Which.Field.Should().Be("query");
    }
}
