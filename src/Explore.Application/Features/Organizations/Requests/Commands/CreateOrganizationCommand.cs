// ABOUTME: MediatR command for creating a new organization.
// ABOUTME: Carries the CreateOrganizationDto payload and pre-create authorization context.
using System.Collections.Generic;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Organization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Organizations.Requests.Commands;

[AuthorizeResource(ResourceKinds.Organization, AuthorizationActions.Create)]
public class CreateOrganizationCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public const string PreCreateResourceId = "create";
    public const string PreCreateAuthorizationPhase = AuthorizationPhases.PreCreate;

    public required CreateOrganizationDto OrganizationDto { get; set; }
    public required Guid CreatorUserId { get; init; }

    string? ISecureRequest.ResourceId => PreCreateResourceId;

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        new PreCreateAuthorizationFacts(Guid.Empty, null, null, null);
}
