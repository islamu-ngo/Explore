// ABOUTME: MediatR command for irreversible administrative redaction of unsafe event content.
// ABOUTME: Uses a dedicated authorization action so heavy moderation does not imply edit authority.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Commands;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ModerateHeavy)]
public sealed class HeavyRedactEventCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public const string DefaultReasonCode = "heavy_redaction";
    public const string StorageDeletionPendingFailureCode = "event_heavy_redaction_storage_deletion_pending";
    public const string UserResolutionFailureCode = "event_heavy_redaction_user_unresolved";

    public Guid Id { get; set; }
    public string ReasonCode { get; set; } = DefaultReasonCode;
    public string? CorrelationId { get; set; }
    public Guid? SourceReportId { get; set; }
    public Guid? SourceReportDecisionId { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
