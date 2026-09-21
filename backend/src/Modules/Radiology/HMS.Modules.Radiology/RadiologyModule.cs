using HMS.Modules.Radiology.Application;
using HMS.Modules.Radiology.Application.Abstractions;
using HMS.Modules.Radiology.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.Radiology;

/// <summary>Single composition entry point for this module, called once from HMS.Api/Configuration.
/// The module owns no database — it reads stored images through Documents' public seam.</summary>
public static class RadiologyModule
{
    public static IServiceCollection AddRadiologyModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Timeout is enforced per call from Ai:Radiology:TimeoutSeconds, not by HttpClient's fixed 100s.
        services.AddHttpClient<IXrayImageAnalyzer, OpenAiCompatibleXrayImageAnalyzer>(client => client.Timeout = Timeout.InfiniteTimeSpan);
        services.AddScoped<IRadiologyAiService, RadiologyAiService>();

        return services;
    }
}
