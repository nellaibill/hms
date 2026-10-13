using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HMS.Shared.Infrastructure;

/// <summary>
/// Overrides the per-value length limit <see cref="QueryStringGuardFilter"/> applies, for one
/// action or every action of a controller — for the rare endpoint whose query value is
/// free-form text rather than a search term or an id (e.g. a natural-language question).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class QueryValueMaxLengthAttribute : Attribute
{
    public QueryValueMaxLengthAttribute(int maxLength)
    {
        MaxLength = maxLength;
    }

    public int MaxLength { get; }
}

/// <summary>
/// Rejects a request whose query string carries a value no list endpoint can use, with a 400
/// VALIDATION.FAILED naming the parameter, before the action runs:
/// a value containing a null character, or one longer than the limit.
/// </summary>
/// <remarks>
/// Query values reach PostgreSQL as text parameters (search terms end up inside ILIKE
/// patterns). PostgreSQL text can't hold a null character, so <c>?search=%00</c> surfaced as a
/// 500 from every list endpoint. And nothing bounded a search term's length, while the cost of
/// matching it grows with every character — a 2,000-character term held a patient-list query
/// for over 12 seconds. Both are checked once here rather than per query class, so a new list
/// endpoint is covered without having to remember either.
/// </remarks>
public sealed class QueryStringGuardFilter : IActionFilter
{
    /// <summary>Long enough for any search term, id, date or sort expression the API accepts.</summary>
    public const int DefaultMaxValueLength = 100;

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var query = context.HttpContext.Request.Query;
        if (query.Count == 0)
        {
            return;
        }

        // Endpoint metadata lists controller-level attributes before action-level ones, so
        // the last match is the most specific.
        var maxLength = context.ActionDescriptor.EndpointMetadata?
            .OfType<QueryValueMaxLengthAttribute>()
            .LastOrDefault()?.MaxLength ?? DefaultMaxValueLength;

        var errors = new List<ValidationErrorItem>();
        foreach (var (name, values) in query)
        {
            foreach (var value in values)
            {
                if (value is null)
                {
                    continue;
                }

                if (value.Contains('\0'))
                {
                    errors.Add(new ValidationErrorItem { Field = FieldName(name), Message = "Contains a character that isn't allowed." });
                    break;
                }

                if (value.Length > maxLength)
                {
                    errors.Add(new ValidationErrorItem { Field = FieldName(name), Message = $"Must be {maxLength} characters or fewer." });
                    break;
                }
            }
        }

        if (errors.Count == 0)
        {
            return;
        }

        context.Result = new BadRequestObjectResult(new ApiErrorResponse
        {
            ErrorCode = "VALIDATION.FAILED",
            Message = "One or more query parameters are invalid.",
            ValidationErrors = errors,
            CorrelationId = context.HttpContext.GetCorrelationId(),
            Timestamp = DateTime.UtcNow,
        });
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    // The name is caller-supplied too: echo only what could be a real parameter name.
    private static string FieldName(string name) =>
        name.Length <= 64 && name.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '.' or '[' or ']') ? name : "query";
}
