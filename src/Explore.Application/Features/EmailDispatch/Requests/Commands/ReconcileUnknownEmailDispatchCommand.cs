// ABOUTME: Tenant-scoped command for explicitly resolving an Unknown SMTP outcome.
// ABOUTME: Carries a delivered/not-delivered decision and bounded evidence into one atomic ledger transition.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EmailDispatch.Requests.Commands;

[AuthorizeResource(ResourceKinds.EmailDispatch, AuthorizationActions.EmailDispatches.Reconcile)]
public sealed class ReconcileUnknownEmailDispatchCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TenantId { get; set; }
    public Guid OutboxId { get; set; }
    public EmailDispatchUnknownReconciliationOutcome Outcome { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ProviderMessageId { get; set; }
    public Guid? ChangedBy { get; set; }

    string? ISecureRequest.ResourceId => OutboxId == Guid.Empty ? null : OutboxId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => TenantId == Guid.Empty
        ? null
        : new Dictionary<string, object>
        {
            ["tenantId"] = TenantId.ToString("D"),
            ["outboxId"] = OutboxId.ToString("D"),
            ["authorizationScope"] = "unknown_reconciliation"
        };
}
