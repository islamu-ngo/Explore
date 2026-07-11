// ABOUTME: Native OpenAPI document transformer that mirrors Swashbuckle Keycloak OAuth2 metadata.
// ABOUTME: Keeps /openapi/event-api.json security components aligned with the transitional Swagger baseline.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Explore.API.OpenApi;

internal sealed class KeycloakOpenApiSecurityTransformer(IConfiguration configuration) : IOpenApiDocumentTransformer
{
    private const string SecuritySchemeName = "Keycloak";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var keycloakAuthorizationUrl = configuration["Keycloak:AuthorizationUrl"];
        if (string.IsNullOrWhiteSpace(keycloakAuthorizationUrl)
            || !Uri.TryCreate(keycloakAuthorizationUrl, UriKind.Absolute, out var authorizationUri))
        {
            return Task.CompletedTask;
        }

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>(StringComparer.Ordinal);
        document.Components.SecuritySchemes[SecuritySchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                Implicit = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = authorizationUri,
                    Scopes = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["openid"] = "openid",
                        ["profile"] = "profile"
                    }
                }
            }
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SecuritySchemeName, document)] = []
        });

        return Task.CompletedTask;
    }
}
