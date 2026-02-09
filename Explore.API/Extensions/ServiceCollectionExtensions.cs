using Explore.API.OpenApi;
using Microsoft.OpenApi;

namespace Explore.API.Extensions;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddSwaggerGenWithAuth(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSwaggerGen(options =>
        {
            options.CustomSchemaIds(id => id.FullName!.Replace('+', '-'));

            // Add schema filter for HAL wrapper types to properly expose inner DTOs
            options.SchemaFilter<HalSchemaFilter>();

            options.AddSecurityDefinition("Keycloak", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    Implicit = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri(configuration["Keycloak:AuthorizationUrl"]!),
                        Scopes = new Dictionary<string, string>
                        {
                            { "openid", "openid" },
                            { "profile", "profile" }
                        }
                    }
                }
            });

            // Swashbuckle 10.x requires a Func<OpenApiDocument, OpenApiSecurityRequirement>
            options.AddSecurityRequirement(document =>
            {
                // Get the security scheme reference from the document
                var securitySchemeRef = new OpenApiSecuritySchemeReference("Keycloak", document);

                return new OpenApiSecurityRequirement
                {
                    { securitySchemeRef, new List<string>() }
                };
            });
        });

        return services;
    }
}
