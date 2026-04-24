// ABOUTME: Query request for computing an operator-visible event-template diff against a target template version.
// ABOUTME: Authorized as a custom-property template sync-diff operation and resolved fully in the Application layer.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventTemplateSync;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventTemplateSync.Queries.GetEventTemplateDiff;

[AuthorizeResource(ResourceKinds.CustomPropertyTemplate, AuthorizationActions.CustomPropertyTemplates.SyncDiff)]
public sealed record GetEventTemplateDiffQuery(
    Guid EventId,
    int TargetTemplateVersion
) : IRequest<BaseCommandResponse<TemplateDiffDto>>, ISecureRequest
{
    string? ISecureRequest.ResourceId => EventId.ToString();
}
