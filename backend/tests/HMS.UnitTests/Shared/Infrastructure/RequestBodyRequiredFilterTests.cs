using FluentAssertions;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace HMS.UnitTests.Shared.Infrastructure;

public class RequestBodyRequiredFilterTests
{
    private sealed class SampleRequest
    {
        public string? Name { get; init; }
    }

    private static ParameterDescriptor BodyParameter(string name = "request", EmptyBodyBehavior emptyBodyBehavior = EmptyBodyBehavior.Default) => new()
    {
        Name = name,
        ParameterType = typeof(SampleRequest),
        BindingInfo = new BindingInfo { BindingSource = BindingSource.Body, EmptyBodyBehavior = emptyBodyBehavior },
    };

    private static ParameterDescriptor QueryParameter(string name) => new()
    {
        Name = name,
        ParameterType = typeof(string),
        BindingInfo = new BindingInfo { BindingSource = BindingSource.Query },
    };

    private static ActionExecutingContext ContextFor(IList<ParameterDescriptor> parameters, Dictionary<string, object?> arguments)
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor { Parameters = parameters });

        return new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), arguments, controller: new object());
    }

    [Fact]
    public void OnActionExecuting_ReturnsBadRequest_WhenBodyArgumentDidNotBind()
    {
        // Malformed JSON / an empty body leaves the body parameter out of ActionArguments entirely.
        var context = ContextFor([BodyParameter()], new Dictionary<string, object?>());

        new RequestBodyRequiredFilter().OnActionExecuting(context);

        var result = context.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = result.Value.Should().BeOfType<ApiErrorResponse>().Subject;
        error.ErrorCode.Should().Be("VALIDATION.FAILED");
        error.Message.Should().Be(RequestBodyRequiredFilter.ErrorMessage);
    }

    [Fact]
    public void OnActionExecuting_ReturnsBadRequest_WhenBodyArgumentIsNull()
    {
        var context = ContextFor([BodyParameter()], new Dictionary<string, object?> { ["request"] = null });

        new RequestBodyRequiredFilter().OnActionExecuting(context);

        context.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void OnActionExecuting_LetsTheActionRun_WhenBodyArgumentBound()
    {
        var context = ContextFor([BodyParameter()], new Dictionary<string, object?> { ["request"] = new SampleRequest { Name = "x" } });

        new RequestBodyRequiredFilter().OnActionExecuting(context);

        context.Result.Should().BeNull();
    }

    [Fact]
    public void OnActionExecuting_LetsTheActionRun_WhenBodyIsExplicitlyOptional()
    {
        var context = ContextFor([BodyParameter(emptyBodyBehavior: EmptyBodyBehavior.Allow)], new Dictionary<string, object?>());

        new RequestBodyRequiredFilter().OnActionExecuting(context);

        context.Result.Should().BeNull();
    }

    [Fact]
    public void OnActionExecuting_IgnoresNonBodyParameters()
    {
        var context = ContextFor([QueryParameter("search")], new Dictionary<string, object?>());

        new RequestBodyRequiredFilter().OnActionExecuting(context);

        context.Result.Should().BeNull();
    }
}
