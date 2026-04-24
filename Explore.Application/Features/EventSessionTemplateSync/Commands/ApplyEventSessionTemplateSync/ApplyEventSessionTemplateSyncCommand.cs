// ABOUTME: Command request for applying an operator-selected subset of an event-session-template diff transactionally.
// ABOUTME: Authorized as a custom-property template sync-apply operation and validated manually inside the handler.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionTemplateSync;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionTemplateSync.Commands.ApplyEventSessionTemplateSync;

[AuthorizeResource(ResourceKinds.CustomPropertyTemplate, AuthorizationActions.CustomPropertyTemplates.SyncApply)]
public sealed record ApplyEventSessionTemplateSyncCommand(
    Guid EventSessionId,
    TemplateSyncPlanDto Plan,
    int BaseProvenanceVersion
) : IRequest<BaseCommandResponse<TemplateSyncOutcomeDto>>, ISecureRequest
{
    string? ISecureRequest.ResourceId => EventSessionId.ToString();
}
