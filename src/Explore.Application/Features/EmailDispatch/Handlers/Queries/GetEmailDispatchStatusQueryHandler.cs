// ABOUTME: Handles Basic Dispatch Mode status queries by mapping EmailDispatchOutbox entities to safe DTOs.
// ABOUTME: Keeps repository boundaries entity-first and strips email content before returning operator status.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EmailDispatch;
using Explore.Application.Features.EmailDispatch.Requests.Queries;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EmailDispatch.Handlers.Queries;

public sealed class GetEmailDispatchStatusQueryHandler
    : IRequestHandler<GetEmailDispatchStatusQuery, BaseCommandResponse<IReadOnlyList<EmailDispatchStatusDto>>>
{
    private const int MaxLimit = 200;
    private readonly IEmailDispatchOutboxRepository _repository;

    public GetEmailDispatchStatusQueryHandler(IEmailDispatchOutboxRepository repository)
    {
        _repository = repository;
    }

    public async Task<BaseCommandResponse<IReadOnlyList<EmailDispatchStatusDto>>> Handle(
        GetEmailDispatchStatusQuery request,
        CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<IReadOnlyList<EmailDispatchStatusDto>>();

        if (request.TenantId == Guid.Empty)
        {
            response.Success = false;
            response.Message = "TenantId is required.";
            response.Errors = ["TenantId is required."];
            return response;
        }

        if (request.Limit < 1 || request.Limit > MaxLimit)
        {
            response.Success = false;
            response.Message = $"Limit must be between 1 and {MaxLimit}.";
            response.Errors = [$"Limit must be between 1 and {MaxLimit}."];
            return response;
        }

        var rows = await _repository.GetStatusRows(request.TenantId, request.Limit, cancellationToken);
        var dtos = rows.Select(row => new EmailDispatchStatusDto
        {
            OutboxId = row.Id,
            TenantId = row.TenantId,
            SourceType = row.SourceType,
            SourceId = row.SourceId,
            DeliveryStatus = row.Status.ToString(),
            AttemptCount = row.AttemptCount,
            NextRetryAt = row.NextAttemptAt,
            LastFailureCategory = row.LastFailureCategory,
            LastFailureAt = row.LastFailureAt,
            UnknownAt = row.UnknownAt,
            DeliveredAt = row.SentAt,
            ParkedAt = row.ParkedAt,
            ContentRedactedAt = row.ContentRedactedAt,
            CorrelationId = row.CorrelationId
        }).ToList();

        response.Success = true;
        response.Id = dtos;
        response.Message = "Email dispatch status retrieved.";

        return response;
    }
}
