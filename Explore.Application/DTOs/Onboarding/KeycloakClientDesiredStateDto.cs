// ABOUTME: Describes a platform-owned Keycloak client requirement for drift planning.
// ABOUTME: Used by realm sync preview to model additive client updates without mutations.

namespace Explore.Application.DTOs.Onboarding;

public class KeycloakClientDesiredStateDto
{
    public string ClientId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ClientKind { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string Protocol { get; set; } = "openid-connect";
    public bool PublicClient { get; set; }
    public bool BearerOnly { get; set; }
    public bool StandardFlowEnabled { get; set; }
    public bool DirectAccessGrantsEnabled { get; set; }
    public bool ServiceAccountsEnabled { get; set; }
    public IReadOnlyList<string> RedirectUris { get; set; } = [];
    public IReadOnlyList<string> WebOrigins { get; set; } = [];
    public IReadOnlyList<string> OptionalClientScopes { get; set; } = [];
    public IReadOnlyList<string> DefaultClientScopes { get; set; } = [];
    public IReadOnlyList<string> RealmRoleMappings { get; set; } = [];
    public IReadOnlyList<KeycloakProtocolMapperDesiredStateDto> ProtocolMappers { get; set; } = [];
}
