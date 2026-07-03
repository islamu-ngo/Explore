// ABOUTME: Handles webhook consumer creation with Application-owned validation and mapping.
// ABOUTME: Persists canonical consumer rows without exposing provider internals to controllers.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Webhooks.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.Webhooks.Handlers.Commands;

public sealed class CreateWebhookConsumerCommandHandler(IWebhookConsumerRepository consumerRepository)
    : IRequestHandler<CreateWebhookConsumerCommand, BaseCommandResponse<Guid>>
{
    private const int MaxNameLength = 200;
    private const int MaxExternalProviderAppIdLength = 500;

    public async Task<BaseCommandResponse<Guid>> Handle(
        CreateWebhookConsumerCommand request,
        CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return Failure(
                "Webhook consumer validation failed.",
                "webhook_consumer_validation_failed",
                errors);
        }

        var name = request.Name.Trim();
        var existing = await consumerRepository.GetByTenantAndNameAsync(
            request.TenantId,
            name,
            cancellationToken);

        if (existing is not null)
        {
            return Failure(
                "Webhook consumer name is already in use.",
                "webhook_consumer_name_conflict",
                ["A webhook consumer with this name already exists for the current tenant."]);
        }

        var consumer = new WebhookConsumer
        {
            Id = Guid.CreateVersion7(),
            TenantId = request.TenantId,
            OwnerActorId = request.OwnerActorId,
            OwnerUserId = request.OwnerUserId,
            ConsumerKind = (WebhookConsumerKind)request.ConsumerKindId,
            Name = name,
            Status = WebhookConsumerStatus.Active,
            ProviderMode = (WebhookProviderMode)request.ProviderModeId,
            ExternalProviderAppId = NormalizeOptional(request.ExternalProviderAppId),
            CreatedAt = DateTime.UtcNow
        };

        var created = await consumerRepository.CreateAsync(consumer, cancellationToken);

        return new BaseCommandResponse<Guid>
        {
            Success = true,
            Message = "Webhook consumer created.",
            Id = created.Id
        };
    }

    private static List<string> Validate(CreateWebhookConsumerCommand request)
    {
        var errors = new List<string>();

        if (request.TenantId == Guid.Empty)
        {
            errors.Add("TenantId is required.");
        }

        if (request.OwnerActorId == Guid.Empty)
        {
            errors.Add("OwnerActorId must not be empty when provided.");
        }

        if (request.OwnerUserId == Guid.Empty)
        {
            errors.Add("OwnerUserId must not be empty when provided.");
        }

        if (request.OwnerActorId.HasValue && request.OwnerUserId.HasValue)
        {
            errors.Add("Only one owner reference may be set.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add("Name is required.");
        }
        else if (request.Name.Trim().Length > MaxNameLength)
        {
            errors.Add($"Name must be {MaxNameLength} characters or fewer.");
        }

        if (!Enum.IsDefined(typeof(WebhookConsumerKind), request.ConsumerKindId))
        {
            errors.Add("ConsumerKindId is invalid.");
        }

        if (!Enum.IsDefined(typeof(WebhookProviderMode), request.ProviderModeId))
        {
            errors.Add("ProviderModeId is invalid.");
        }

        if (request.ExternalProviderAppId is { } externalProviderAppId &&
            externalProviderAppId.Trim().Length > MaxExternalProviderAppIdLength)
        {
            errors.Add($"ExternalProviderAppId must be {MaxExternalProviderAppIdLength} characters or fewer.");
        }

        return errors;
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static BaseCommandResponse<Guid> Failure(
        string message,
        string failureCode,
        List<string> errors)
        => new()
        {
            Success = false,
            Message = message,
            FailureCode = failureCode,
            Errors = errors
        };
}
