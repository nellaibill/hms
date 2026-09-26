using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Shared.Infrastructure.Ai;

public enum AiProvider
{
    Anthropic,
    OpenAI,
    AzureOpenAI,
    HuggingFace,
}

public static class AiServiceCollectionExtensions
{
    /// <summary>The provider named by <c>Ai:Provider</c> (case-insensitive: "OpenAI", "AzureOpenAI", "HuggingFace");
    /// anything else, including unset, means Anthropic so existing deployments keep working.</summary>
    public static AiProvider GetAiProvider(this IConfiguration configuration) =>
        Enum.TryParse<AiProvider>(configuration["Ai:Provider"], ignoreCase: true, out var provider) ? provider : AiProvider.Anthropic;

    /// <summary>
    /// Registers the one <see cref="IAiStructuredExtractor"/> chosen by <c>Ai:Provider</c>. A
    /// per-deployment choice made once at startup, not a per-request one. Idempotent, so any
    /// module that needs the extractor can call it without coordinating with the others.
    /// </summary>
    public static IServiceCollection AddHmsAiExtractor(this IServiceCollection services, IConfiguration configuration)
    {
        if (services.Any(descriptor => descriptor.ServiceType == typeof(IAiStructuredExtractor)))
        {
            return services;
        }

        switch (configuration.GetAiProvider())
        {
            case AiProvider.OpenAI:
                services.AddHttpClient<IAiStructuredExtractor, OpenAiStructuredExtractor>();
                break;
            case AiProvider.AzureOpenAI:
                services.AddHttpClient<IAiStructuredExtractor, AzureOpenAiStructuredExtractor>();
                break;
            case AiProvider.HuggingFace:
                services.AddHttpClient<IAiStructuredExtractor, HuggingFaceStructuredExtractor>();
                break;
            default:
                services.AddHttpClient<IAiStructuredExtractor, AnthropicStructuredExtractor>();
                break;
        }

        return services;
    }

    /// <summary>
    /// Registers the <see cref="IAiEmbeddingProvider"/> named by <c>Ai:Embeddings:Provider</c>
    /// ("HuggingFace"); unset or anything else registers a disabled provider, so embedding is
    /// opt-in per environment. Independent of <c>Ai:Provider</c> — see IAiEmbeddingProvider.
    /// Idempotent, like <see cref="AddHmsAiExtractor"/>.
    /// </summary>
    public static IServiceCollection AddHmsAiEmbeddings(this IServiceCollection services, IConfiguration configuration)
    {
        if (services.Any(descriptor => descriptor.ServiceType == typeof(IAiEmbeddingProvider)))
        {
            return services;
        }

        if (string.Equals(configuration["Ai:Embeddings:Provider"], "HuggingFace", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IAiEmbeddingProvider, HuggingFaceEmbeddingProvider>();
        }
        else
        {
            services.AddSingleton<IAiEmbeddingProvider, DisabledEmbeddingProvider>();
        }

        return services;
    }
}
