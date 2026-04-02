// ABOUTME: MediatR command for updating organization profile details.
// ABOUTME: Carries the detailed UpdateOrganizationDetailsDto payload.
using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Organization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Organizations.Requests.Commands;

[AuthorizeResource("organization", AuthorizationActions.Update)]
public class UpdateOrganizationDetailsCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
    public required UpdateOrganizationDto OrganizationDto { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
