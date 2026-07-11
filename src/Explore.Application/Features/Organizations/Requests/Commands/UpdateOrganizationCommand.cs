// ABOUTME: MediatR command for PATCH-based Organization profile updates.
// ABOUTME: Carries route authority, current user authorization context, If-Match concurrency, and grouped payload.
using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Organization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Organizations.Requests.Commands;

[AuthorizeResource(ResourceKinds.Organization, AuthorizationActions.Update)]
public class UpdateOrganizationCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid OrganizationId { get; set; }

    public required string UserId { get; set; }

    public Guid ExpectedConcurrencyStamp { get; set; }

    public required UpdateOrganizationDto UpdateOrganizationDto { get; set; }

    string? ISecureRequest.ResourceId => OrganizationId.ToString();
}
