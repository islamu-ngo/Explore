// ABOUTME: MediatR command for updating top-level organization metadata.
// ABOUTME: Carries the UpdateOrganizationDto payload.
using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Organization;
using MediatR;

namespace Explore.Application.Features.Organizations.Requests.Commands;

[AuthorizeResource("organization", PermissionAction.Update)]
public class UpdateOrganizationCommand : IRequest<Unit>, ISecureRequest
{
    public Guid Id { get; set; }
    public required UpdateOrganizationApprovalStatusDto OrganizationApprovalStatusDto { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
