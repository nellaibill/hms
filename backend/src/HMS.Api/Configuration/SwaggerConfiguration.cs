using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi;

namespace HMS.Api.Configuration;

/// <summary>
/// Swagger/OpenAPI generation for the whole host, via Swashbuckle. Registered here (not
/// ModuleRegistration) since it's host-level API documentation, not a business module.
/// The JWT Bearer scheme wires up the Swagger UI "Authorize" button so it's ready the
/// moment real authentication ships — no auth is enforced yet.
/// </summary>
public static class SwaggerConfiguration
{
    private const string JwtSecurityScheme = "Bearer";

    public static IServiceCollection AddHmsSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "HMS API",
                Version = "v1",
                Description = "Hospital Management System API",
            });

            // Groups endpoints by module (e.g. "Identity") rather than by controller name,
            // so grouping stays correct as more modules add controllers over time.
            options.TagActionsBy(api =>
            {
                if (api.ActionDescriptor is ControllerActionDescriptor descriptor &&
                    descriptor.ControllerTypeInfo.Namespace?.Split('.') is ["HMS", "Modules", var moduleName, ..])
                {
                    return new[] { moduleName };
                }

                return new[] { api.ActionDescriptor.RouteValues.TryGetValue("controller", out var controller) ? controller! : "Default" };
            });

            // Picks up HMS.Api.xml, HMS.Modules.Identity.xml, and any future module's xml
            // automatically, as long as that project has GenerateDocumentationFile set.
            foreach (var xmlFile in Directory.GetFiles(AppContext.BaseDirectory, "HMS.*.xml"))
            {
                options.IncludeXmlComments(xmlFile, includeControllerXmlComments: true);
            }

            options.AddSecurityDefinition(JwtSecurityScheme, new OpenApiSecurityScheme
            {
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "JWT Bearer token. Authentication is not implemented yet — " +
                    "this scheme only prepares the Authorize button for future JWT integration.",
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(JwtSecurityScheme, document)] = [],
            });
        });

        return services;
    }

    public static WebApplication UseHmsSwagger(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "HMS API v1");
        });

        return app;
    }
}
