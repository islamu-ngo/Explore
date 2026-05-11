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
            options.SwaggerDoc("v0.1", new OpenApiInfo
            {
                Title = "Explore API",
                Version = "v0.1"
            });

            options.CustomSchemaIds(id => id.FullName!.Replace('+', '-'));

            // Add schema filter for HAL wrapper types to properly expose inner DTOs
            options.SchemaFilter<HalSchemaFilter>();
            options.SchemaFilter<OpenApiStringEnumSchemaFilter>();
            options.DocumentFilter<HalSchemaDocumentFilter>();

            // Swashbuckle remains as a transition baseline until native OpenAPI parity is proven.
            // Mirror the native document's media-type version aliases so both documents describe
            // the same public content negotiation contract.
            options.OperationFilter<OpenApiVersionedContentTypesOperationFilter>();

            // Register the Keycloak OAuth2 security definition only when the authorization URL
            // is configured. In test/dev environments where Keycloak is not wired up, we skip this
            // block so that OpenAPI generation still succeeds (enabling contract-invariant tests
            // and local swagger browsing to work without a live identity provider).
            var keycloakAuthorizationUrl = configuration["Keycloak:AuthorizationUrl"];
            if (!string.IsNullOrWhiteSpace(keycloakAuthorizationUrl))
            {
                options.AddSecurityDefinition("Keycloak", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        Implicit = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri(keycloakAuthorizationUrl),
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
            }
        });

        return services;
    }
}
