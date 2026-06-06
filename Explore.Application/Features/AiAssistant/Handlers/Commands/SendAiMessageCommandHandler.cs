// ABOUTME: Orchestrates guarded AI assistant message sends after tenant and provider readiness checks.
// ABOUTME: Persists user/assistant messages, runs, and proposed actions without executing tools or event side effects.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Infrastructure.Ai;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Ai.Validators;
using Explore.Application.Features.AiAssistant.Prompting;
using Explore.Application.Features.AiAssistant.Requests.Commands;
using Explore.Application.Features.AiAssistant.Tools;
using Explore.Application.Responses;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Domain;
using Explore.Domain.Ai;
using Explore.Domain.Constants;
using MediatR;

namespace Explore.Application.Features.AiAssistant.Handlers.Commands;

public sealed class SendAiMessageCommandHandler : IRequestHandler<SendAiMessageCommand, BaseCommandResponse<Guid>>
{
    private const string IdempotencyMethod = "AI_SEND";
    private const string JsonContentType = "application/json";
    private const string TextContentType = "text/plain";
    private const string UserDailyQuotaScope = "user_daily";
    private const string TenantDailyQuotaScope = "tenant_daily";
    private const string UserConcurrentQuotaScope = "user_concurrent";
    private readonly IAiConversationRepository _conversationRepository;
    private readonly IIdempotencyRepository _idempotencyRepository;
    private readonly IHierarchicalSettingsResolver _settingsResolver;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAiModelCatalog _modelCatalog;
    private readonly AiPromptContextBuilder _promptContextBuilder;
    private readonly AiProviderResponseResolver _providerResponseResolver;

    public SendAiMessageCommandHandler(
        IAiConversationRepository conversationRepository,
        IIdempotencyRepository idempotencyRepository,
        IHierarchicalSettingsResolver settingsResolver,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService,
        IAiModelCatalog modelCatalog,
        IAiChatProvider chatProvider)
    {
        _conversationRepository = conversationRepository;
        _idempotencyRepository = idempotencyRepository;
        _settingsResolver = settingsResolver;
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
        _modelCatalog = modelCatalog;
        var toolRegistry = AiToolContractRegistry.CreateDefault();
        _promptContextBuilder = new AiPromptContextBuilder(new AiSystemPromptFactory(toolRegistry));
        _providerResponseResolver = new AiProviderResponseResolver(
            chatProvider,
            new AiStructuredActionParser(toolRegistry));
    }

    public async Task<BaseCommandResponse<Guid>> Handle(
        SendAiMessageCommand request,
        CancellationToken cancellationToken)
    {
        var validator = new SendAiMessageRequestDtoValidator();
        var validationResult = await validator.ValidateAsync(request.Message, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Failure(
                "AI message send failed.",
                validationResult.Errors.Select(error => error.ErrorMessage),
                "validation_failed");
        }

        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is not { } userId)
        {
            return Failure("AI messages require an authenticated user.", ["User is not authenticated."], "unauthenticated");
        }

        var tenantId = _tenantContext.TenantId;
        var content = request.Message.Content.Trim();
        var idempotencyKey = request.Message.IdempotencyKey.Trim();
        var requestTarget = $"ai/conversations/{request.ConversationId:N}/messages";
        var requestBodyHash = ComputeBodyHash(request.ConversationId, content);
        var principalFingerprint = ComputePrincipalFingerprint(userId);

        var replay = await TryReplayIdempotencyAsync(
            idempotencyKey,
            tenantId,
            userId,
            requestTarget,
            requestBodyHash,
            principalFingerprint,
            cancellationToken);

        if (replay is not null)
        {
            return replay;
        }

        var settings = await _settingsResolver.ResolveGroupAsync<AiAssistantSettingGroup>(
            new SettingContext(TenantId: tenantId), cancellationToken);
        var disabledReason = AiAssistantAvailability.ResolveDisabledReason(settings);

        if (disabledReason is not null)
        {
            return Failure(
                "AI assistant is not available for this tenant.",
                [$"AI assistant is unavailable: {disabledReason}."],
                disabledReason);
        }

        var modelId = AiAssistantAvailability.ResolveModelId(settings);
        var provider = AiAssistantAvailability.NormalizeProvider(settings.Provider);
        var availableModels = await _modelCatalog.ListAvailableModelsAsync(cancellationToken);

        if (!availableModels.Any(model => string.Equals(model.Id, modelId, StringComparison.Ordinal)))
        {
            return Failure(
                "AI provider is not ready for this tenant model.",
                ["Configured AI model is not available from the runtime provider."],
                "provider_not_ready");
        }

        var conversation = await _conversationRepository.GetByIdForUpdateAsync(request.ConversationId, cancellationToken);

        if (conversation is null || conversation.UserId != userId)
        {
            return Failure("AI conversation was not found.", ["Conversation was not found."], "conversation_not_found");
        }

        if (conversation.Status != AiConversationStatus.Active)
        {
            return Failure(
                "AI conversation is not ready for a new message.",
                [$"Conversation status is {conversation.Status}."],
                "conversation_not_active");
        }

        var todayUtc = DateTime.UtcNow.Date;
        var messageCount = await _conversationRepository.CountUserMessagesSinceAsync(userId, todayUtc, cancellationToken);

        if (messageCount >= settings.DailyMessageLimit)
        {
            var response = new BaseCommandResponse<Guid>();
            response.SetQuotaExceeded(
                "AI daily message limit exceeded.",
                new QuotaExceededDetails(
                    GovernanceSettingKeys.AiAssistant.DailyMessageLimit,
                    settings.DailyMessageLimit,
                    messageCount,
                    messageCount + 1,
                    UserDailyQuotaScope,
                    tenantId),
                "Daily AI message quota exceeded.");
            return response;
        }

        var tenantMessageCount = await _conversationRepository.CountTenantMessagesSinceAsync(todayUtc, cancellationToken);

        if (tenantMessageCount >= settings.DailyTenantMessageLimit)
        {
            var response = new BaseCommandResponse<Guid>();
            response.SetQuotaExceeded(
                "AI tenant daily message limit exceeded.",
                new QuotaExceededDetails(
                    GovernanceSettingKeys.AiAssistant.DailyTenantMessageLimit,
                    settings.DailyTenantMessageLimit,
                    tenantMessageCount,
                    tenantMessageCount + 1,
                    TenantDailyQuotaScope,
                    tenantId),
                "Daily tenant AI message quota exceeded.");
            return response;
        }

        var runningConversationCount = await _conversationRepository.CountRunningConversationsForUserAsync(userId, cancellationToken);

        if (runningConversationCount >= settings.ConcurrentRunLimit)
        {
            var response = new BaseCommandResponse<Guid>();
            response.SetQuotaExceeded(
                "AI concurrent run limit exceeded.",
                new QuotaExceededDetails(
                    GovernanceSettingKeys.AiAssistant.ConcurrentRunLimit,
                    settings.ConcurrentRunLimit,
                    runningConversationCount,
                    runningConversationCount + 1,
                    UserConcurrentQuotaScope,
                    tenantId),
                "Concurrent AI run quota exceeded.");
            return response;
        }

        var utcNow = DateTime.UtcNow;
        var userMessage = conversation.AddMessage(AiMessageRole.User, content, userId, utcNow);
        var run = conversation.QueueRun(provider, modelId, utcNow);
        run.Start(utcNow);
        await _conversationRepository.Update(conversation);

        var providerResolution = await _providerResponseResolver.ResolveAsync(
            _promptContextBuilder.Build(conversation, settings, modelId),
            cancellationToken);

        if (!providerResolution.Succeeded || providerResolution.Response is null || providerResolution.ParseResult is null)
        {
            var errorCode = providerResolution.FailureCode ?? "provider_failure";
            var errorMessage = providerResolution.FailureMessage ?? "AI provider failed.";
            conversation.FailRun(run, errorCode, errorMessage, DateTime.UtcNow);
            await _conversationRepository.Update(conversation);
            return Failure("AI provider failed to complete the run.", [errorMessage], errorCode, run.Id);
        }

        var assistantText = BuildAssistantMessage(providerResolution.Response);
        var assistantMessage = conversation.AddMessage(AiMessageRole.Assistant, assistantText, null, DateTime.UtcNow);

        foreach (var proposedAction in providerResolution.ParseResult.Actions)
        {
            conversation.ProposeAction(
                proposedAction.Kind,
                proposedAction.PayloadJson,
                assistantMessage.Id,
                userId,
                DateTime.UtcNow);
        }

        conversation.CompleteRun(run, DateTime.UtcNow);
        await _conversationRepository.Update(conversation);
        await SaveIdempotencyAsync(
            idempotencyKey,
            tenantId,
            userId,
            requestTarget,
            requestBodyHash,
            principalFingerprint,
            run.Id,
            cancellationToken);

        return new BaseCommandResponse<Guid>
        {
            Success = true,
            Id = run.Id,
            Message = "AI message sent."
        };
    }

    private async Task<BaseCommandResponse<Guid>?> TryReplayIdempotencyAsync(
        string idempotencyKey,
        Guid tenantId,
        Guid userId,
        string requestTarget,
        string requestBodyHash,
        string principalFingerprint,
        CancellationToken cancellationToken)
    {
        var existing = await _idempotencyRepository.FindAsync(idempotencyKey, tenantId, cancellationToken);

        if (existing is null)
        {
            return null;
        }

        if (existing.UserId != userId.ToString("N", CultureInfo.InvariantCulture) ||
            existing.RequestMethod != IdempotencyMethod ||
            existing.RequestTarget != requestTarget ||
            existing.RequestBodyHash != requestBodyHash ||
            existing.PrincipalFingerprint != principalFingerprint)
        {
            return Failure(
                "Idempotency key conflicts with a previous AI request.",
                ["Idempotency key was already used for a different request."],
                "idempotency_key_conflict");
        }

        if (Guid.TryParse(existing.ResponseBody, out var runId))
        {
            return new BaseCommandResponse<Guid>
            {
                Success = true,
                Id = runId,
                Message = "AI message send already processed."
            };
        }

        return Failure(
            "Idempotency record cannot be replayed.",
            ["Stored AI idempotency result was invalid."],
            "idempotency_replay_failed");
    }

    private async Task SaveIdempotencyAsync(
        string idempotencyKey,
        Guid tenantId,
        Guid userId,
        string requestTarget,
        string requestBodyHash,
        string principalFingerprint,
        Guid runId,
        CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        await _idempotencyRepository.SaveAsync(new IdempotencyRecord
        {
            Id = Guid.CreateVersion7(),
            Key = idempotencyKey,
            TenantId = tenantId,
            UserId = userId.ToString("N", CultureInfo.InvariantCulture),
            RequestMethod = IdempotencyMethod,
            RequestTarget = requestTarget,
            RequestContentType = JsonContentType,
            RequestBodyHash = requestBodyHash,
            PrincipalFingerprint = principalFingerprint,
            StatusCode = 202,
            ResponseBody = runId.ToString("D", CultureInfo.InvariantCulture),
            ContentType = TextContentType,
            CreatedAt = utcNow,
            ExpiresAt = utcNow.AddHours(24)
        }, cancellationToken);
    }

    private static string BuildAssistantMessage(AiChatResponse response)
    {
        if (!string.IsNullOrWhiteSpace(response.AssistantMessage))
        {
            return response.AssistantMessage;
        }

        return response.ProposedActions.Count > 0
            ? "I prepared a proposed action for your review."
            : "The AI provider returned an empty response.";
    }

    private static string ComputeBodyHash(Guid conversationId, string content)
    {
        var value = $"{conversationId:N}:{content}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private static string ComputePrincipalFingerprint(Guid userId)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(userId.ToString("N", CultureInfo.InvariantCulture))))
            .ToLowerInvariant();

    private static BaseCommandResponse<Guid> Failure(
        string message,
        IEnumerable<string> errors,
        string? failureCode = null,
        Guid id = default)
        => new()
        {
            Success = false,
            Id = id,
            Message = message,
            Errors = errors.ToList(),
            FailureCode = failureCode
        };

}
