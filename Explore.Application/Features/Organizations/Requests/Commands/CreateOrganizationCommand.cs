// ABOUTME: MediatR command for creating a new organization.
// ABOUTME: Carries the CreateOrganizationDto payload.
using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Organization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Organizations.Requests.Commands;

[AuthorizeResource("organization", AuthorizationActions.Create)]
public class CreateOrganizationCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateOrganizationDto OrganizationDto { get; set; }
    public string? UserId { get; set; }

    string? ISecureRequest.ResourceId => null;
}
