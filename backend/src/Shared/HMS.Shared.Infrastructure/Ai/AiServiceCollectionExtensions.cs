using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Shared.Infrastructure.Ai;

public static class AiServiceCollectionExtensions
{
    /// <summary>
    /// Registers the one <see cref="IAiStructuredExtractor"/> chosen by <c>Ai:Provider</c> ("OpenAI",
    /// case-insensitive; anything else, including unset, means Anthropic). A per-deployment choice made
    /// once at startup, not a per-request one. Idempotent, so any module that needs the extractor can
    /// call it without coordinating with the others.
    /// </summary>
    public static IServiceCollection AddHmsAiExtractor(this IServiceCollection services, IConfiguration configuration)
    {
        if (services.Any(descriptor => descriptor.ServiceType == typeof(IAiStructuredExtractor)))
        {
            return services;
        }

        if (string.Equals(configuration["Ai:Provider"], "OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IAiStructuredExtractor, OpenAiStructuredExtractor>();
        }
        else
        {
            services.AddHttpClient<IAiStructuredExtractor, AnthropicStructuredExtractor>();
        }

        return services;
    }
}
