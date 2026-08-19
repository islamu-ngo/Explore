// ABOUTME: MediatR command for creating a new category.
// ABOUTME: Carries the CreateCategoryDto payload.
using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Category;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Categories.Requests.Commands;

[AuthorizeResource(ResourceKinds.Category, AuthorizationActions.Create)]
public class CreateCategoryCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateCategoryDto CategoryDto { get; set; }

    string? ISecureRequest.ResourceId => null;
    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        new TenantScopedAuthorizationFacts(CategoryDto.TenantId);
}
