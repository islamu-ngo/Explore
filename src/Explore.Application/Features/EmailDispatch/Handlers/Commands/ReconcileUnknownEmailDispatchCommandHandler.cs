// ABOUTME: Reconciles an Unknown SMTP outcome through one durable delivered/not-delivered transaction.
// ABOUTME: Rejects stale, redacted, or non-Unknown rows before updating every delivery ledger.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EmailDispatch.Requests.Commands;
using Explore.Application.Features.EmailDispatch.Validators;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.EmailDispatch.Handlers.Commands;

public sealed class ReconcileUnknownEmailDispatchCommandHandler(
    IEmailDispatchOutboxRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReconcileUnknownEmailDispatchCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        ReconcileUnknownEmailDispatchCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await new ReconcileUnknownEmailDispatchCommandValidator()
            .ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Failure(
                "Email dispatch reconciliation failed validation.",
                EmailDispatchFailureCodes.ValidationFailed,
                validation.Errors.Select(error => error.ErrorMessage));
        }

        var dispatch = await repository.GetByTenantAndId(request.TenantId, request.OutboxId, cancellationToken);
        if (dispatch is null)
        {
            return Failure("Email dispatch outbox row was not found.", EmailDispatchFailureCodes.NotFound,
                ["Email dispatch outbox row was not found."]);
        }

        if (dispatch.ContentRedactedAt is not null || dispatch.Status != EmailDispatchStatus.Unknown)
        {
            return Failure(
                "Only non-redacted Unknown email dispatch rows can be reconciled.",
                EmailDispatchFailureCodes.InvalidTransition,
                ["Only non-redacted Unknown email dispatch rows can be reconciled."]);
        }

        var changed = await unitOfWork.ExecuteInTransactionAsync(
            ct => repository.TryReconcileUnknown(
                request.TenantId,
                request.OutboxId,
                request.Outcome,
                request.Reason.Trim(),
                string.IsNullOrWhiteSpace(request.ProviderMessageId) ? null : request.ProviderMessageId.Trim(),
                request.ChangedBy,
                DateTime.UtcNow,
                ct),
            cancellationToken);

        return changed
            ? new BaseCommandResponse<Guid>
            {
                Id = dispatch.Id,
                Success = true,
                Message = request.Outcome == EmailDispatchUnknownReconciliationOutcome.Delivered
                    ? "Unknown email dispatch reconciled as delivered."
                    : "Unknown email dispatch reconciled as not delivered and queued."
            }
            : Failure(
                "Email dispatch state changed before it could be reconciled.",
                EmailDispatchFailureCodes.ConcurrentTransition,
                ["Email dispatch state changed before it could be reconciled."]);
    }

    private static BaseCommandResponse<Guid> Failure(
        string message,
        string failureCode,
        IEnumerable<string> errors) => new()
        {
            Success = false,
            Message = message,
            FailureCode = failureCode,
            Errors = errors.ToList()
        };
}
