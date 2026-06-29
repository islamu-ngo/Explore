// ABOUTME: MediatR command for route-ID event-category link updates.
// ABOUTME: Carries expected concurrency and grouped relationship update payload.
using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.EventCategories;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventCategories.Requests.Commands;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Update)]
public class UpdateEventCategoriesCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid EventCategoryId { get; set; }
    public Guid ExpectedConcurrencyStamp { get; set; }
    public required UpdateEventCategoriesDto EventCategoriesDto { get; set; }
    public Guid EventId { get; set; }
    public Guid TenantId { get; set; }

    string? ISecureRequest.ResourceId => EventId.ToString();
}
