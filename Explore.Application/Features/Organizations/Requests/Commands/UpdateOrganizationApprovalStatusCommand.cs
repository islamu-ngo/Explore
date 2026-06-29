// ABOUTME: MediatR command for the admin-only organization approval status action.
// ABOUTME: Keeps approval lifecycle changes separate from grouped profile property updates.
using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Organization;
using MediatR;

namespace Explore.Application.Features.Organizations.Requests.Commands;

[AuthorizeResource(ResourceKinds.Organization, AuthorizationActions.Update)]
public class UpdateOrganizationApprovalStatusCommand : IRequest<Unit>, ISecureRequest
{
    public Guid OrganizationId { get; set; }

    public required UpdateOrganizationApprovalStatusDto ApprovalStatusDto { get; set; }

    string? ISecureRequest.ResourceId => OrganizationId.ToString();
}
