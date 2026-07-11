// ABOUTME: Command request for applying an operator-selected subset of an event-template diff transactionally.
// ABOUTME: Authorized as a custom-property template sync-apply operation and validated manually inside the handler.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventTemplateSync;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventTemplateSync.Commands.ApplyEventTemplateSync;

[AuthorizeResource(ResourceKinds.CustomPropertyTemplate, AuthorizationActions.CustomPropertyTemplates.SyncApply)]
public sealed record ApplyEventTemplateSyncCommand(
    Guid EventId,
    TemplateSyncPlanDto Plan,
    int BaseProvenanceVersion
) : IRequest<BaseCommandResponse<TemplateSyncOutcomeDto>>, ISecureRequest
{
    string? ISecureRequest.ResourceId => EventId.ToString();
}
