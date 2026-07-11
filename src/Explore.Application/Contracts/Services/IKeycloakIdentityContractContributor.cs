// ABOUTME: Application-layer contributor contract for Keycloak identity requirements.
// ABOUTME: Lets future modules add desired clients/scopes/mappings without owning the whole realm.

using Explore.Application.DTOs.Onboarding;

namespace Explore.Application.Contracts.Services;

public interface IKeycloakIdentityContractContributor
{
    string ContractName { get; }

    void Contribute(KeycloakRealmDesiredStateDto desiredState, KeycloakRealmDesiredStateBuildRequestDto request);
}
