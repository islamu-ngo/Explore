// ABOUTME: MediatR command for PATCH-based category updates.
// ABOUTME: Carries route authority, If-Match concurrency, and the grouped update payload.
using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Category;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Categories.Requests.Commands;

[AuthorizeResource(ResourceKinds.Category, AuthorizationActions.Update)]
public class UpdateCategoryCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid CategoryId { get; set; }

    public Guid ExpectedConcurrencyStamp { get; set; }

    public required UpdateCategoryDto UpdateCategoryDto { get; set; }

    string? ISecureRequest.ResourceId => CategoryId.ToString();
}
