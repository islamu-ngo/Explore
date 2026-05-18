// ABOUTME: MediatR command for updating an existing category.
// ABOUTME: Carries the UpdateCategoryDto payload.
using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Category;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Categories.Requests.Commands;

[AuthorizeResource(ResourceKinds.Category, AuthorizationActions.Update)]
public class UpdateCategoryCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UpdateCategoryDto CategoryDto { get; set; }

    string? ISecureRequest.ResourceId => CategoryDto.Id.ToString();
}
