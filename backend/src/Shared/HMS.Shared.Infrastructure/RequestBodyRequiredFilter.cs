using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HMS.Shared.Infrastructure;

/// <summary>
/// Rejects any request whose <c>[FromBody]</c> argument didn't bind — a missing body, malformed
/// JSON, or JSON that can't be deserialized into the request type — with a 400 VALIDATION.FAILED.
/// </summary>
/// <remarks>
/// Program.cs suppresses ASP.NET Core's own ModelState 400 so controllers can run FluentValidation
/// themselves and keep one error shape. The cost is that an unbound body reaches the action as
/// <c>null</c>, and the first <c>ValidateAsync(null)</c> throws — a 500 for what is really bad
/// input. A handful of controllers (HR, Billing, Discharge Summary) guarded this per action; this
/// filter applies the same guard, with the same message, to every action. A body parameter that
/// opts in with <c>EmptyBodyBehavior.Allow</c> is left alone, since null is a valid value there.
/// </remarks>
public sealed class RequestBodyRequiredFilter : IActionFilter
{
    public const string ErrorMessage = "The request body is missing or could not be parsed.";

    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var parameter in context.ActionDescriptor.Parameters)
        {
            var bindingInfo = parameter.BindingInfo;
            if (bindingInfo?.BindingSource != BindingSource.Body || bindingInfo.EmptyBodyBehavior == EmptyBodyBehavior.Allow)
            {
                continue;
            }

            if (context.ActionArguments.TryGetValue(parameter.Name, out var value) && value is not null)
            {
                continue;
            }

            context.Result = new BadRequestObjectResult(new ApiErrorResponse
            {
                ErrorCode = "VALIDATION.FAILED",
                Message = ErrorMessage,
                CorrelationId = context.HttpContext.GetCorrelationId(),
                Timestamp = DateTime.UtcNow,
            });
            return;
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
