// ABOUTME: MediatR command for creating a new category.
// ABOUTME: Carries the CreateCategoryDto payload.
using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Category;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Categories.Requests.Commands;

[AuthorizeResource("category", AuthorizationActions.Create)]
public class CreateCategoryCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateCategoryDto CategoryDto { get; set; }

    string? ISecureRequest.ResourceId => null;
    IDictionary<string, object>? ISecureRequest.ResourceAttributes =>
        CategoryDto.TenantId != Guid.Empty
            ? new Dictionary<string, object> { ["tenantId"] = CategoryDto.TenantId.ToString() }
            : null;
}
