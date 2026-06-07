// ABOUTME: Blazor service wrapper around generated AI assistant API client methods.
// ABOUTME: Provides safe defaults and preserves HAL resources for UI affordance gating.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Ai;

namespace Explore.Blazor.Client.Services.Ai;

public sealed class AiAssistantClientService(
    IEventApiClient apiClient,
    ILogger<AiAssistantClientService> logger) : IAiAssistantClientService
{
    public async Task<HalResourceOfAiAssistantBootstrapDto?> GetBootstrapAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await apiClient.GetAiAssistantBootstrapAsync(cancellationToken: cancellationToken);
        }
        catch (ApiException ex)
        {
            logger.LogWarning(ex, "Failed to load AI assistant bootstrap.");
            return null;
        }
    }

    public async Task<HalCollectionResourceOfAiConversationSummaryDto?> GetConversationCollectionAsync(
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await apiClient.GetAiConversationsAsync(limit, cancellationToken: cancellationToken);
        }
        catch (ApiException ex)
        {
            logger.LogWarning(ex, "Failed to load AI assistant conversations.");
            return null;
        }
    }

    public async Task<IReadOnlyList<HalResourceOfAiConversationSummaryDto>> GetConversationsAsync(
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var response = await GetConversationCollectionAsync(limit, cancellationToken);
        return response?._embedded?.Items?.ToList() ?? [];
    }

    public async Task<HalResourceOfAiConversationDto?> GetConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await apiClient.GetAiConversationAsync(conversationId, cancellationToken: cancellationToken);
        }
        catch (ApiException ex)
        {
            logger.LogWarning(ex, "Failed to load AI assistant conversation {ConversationId}.", conversationId);
            return null;
        }
    }

    public async Task<AiAssistantCommandResult> CreateConversationAsync(
        CreateAiConversationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await apiClient.CreateAiConversationAsync(request, cancellationToken: cancellationToken);
            return AiAssistantCommandResult.FromResponse(response);
        }
        catch (ApiException<ProblemDetails> ex)
        {
            logger.LogWarning(ex, "Failed to create AI assistant conversation.");
            return AiAssistantCommandResult.Failure(
                FailureCodeFor(ex),
                ex.Result?.Detail ?? ex.Result?.Title ?? "The AI assistant conversation could not be created.");
        }
        catch (ApiException ex)
        {
            logger.LogWarning(ex, "Failed to create AI assistant conversation.");
            return AiAssistantCommandResult.Failure(
                FailureCodeFor(ex),
                "The AI assistant conversation could not be created.");
        }
    }

    public async Task<AiAssistantCommandResult> SendMessageAsync(
        Guid conversationId,
        string content,
        string? modelId = null,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new SendAiMessageRequestDto
            {
                Content = content,
                IdempotencyKey = idempotencyKey,
                ModelId = modelId
            };

            var response = await apiClient.SendAiMessageAsync(
                conversationId,
                request,
                idempotencyKey,
                cancellationToken: cancellationToken);

            return AiAssistantCommandResult.FromResponse(response);
        }
        catch (ApiException ex)
        {
            logger.LogWarning(ex, "Failed to send AI assistant message for conversation {ConversationId}.", conversationId);
            return AiAssistantCommandResult.Failure("api_error", "The AI assistant message could not be sent.");
        }
    }

    public async Task<IReadOnlyList<HalResourceOfAiReferenceSearchResultDto>> SearchReferencesAsync(
        string searchTerm,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await apiClient.SearchAiReferencesAsync(searchTerm, limit, cancellationToken: cancellationToken);
            return response?._embedded?.Items?.ToList() ?? [];
        }
        catch (ApiException ex)
        {
            logger.LogWarning(ex, "Failed to search AI assistant references.");
            return [];
        }
    }

    public async Task<AiAssistantCommandResult> ConfirmProposedActionAsync(
        Guid conversationId,
        Guid proposedActionId,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await apiClient.ConfirmAiProposedActionAsync(
                conversationId,
                proposedActionId,
                idempotencyKey,
                cancellationToken: cancellationToken);

            return AiAssistantCommandResult.FromResponse(response);
        }
        catch (ApiException ex)
        {
            logger.LogWarning(
                ex,
                "Failed to confirm AI proposed action {ProposedActionId} for conversation {ConversationId}.",
                proposedActionId,
                conversationId);

            return AiAssistantCommandResult.Failure("api_error", "The AI proposed action could not be confirmed.");
        }
    }

    public async Task<AiAssistantCommandResult> RejectProposedActionAsync(
        Guid conversationId,
        Guid proposedActionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await apiClient.RejectAiProposedActionAsync(
                conversationId,
                proposedActionId,
                cancellationToken: cancellationToken);

            return AiAssistantCommandResult.FromResponse(response);
        }
        catch (ApiException ex)
        {
            logger.LogWarning(
                ex,
                "Failed to reject AI proposed action {ProposedActionId} for conversation {ConversationId}.",
                proposedActionId,
                conversationId);

            return AiAssistantCommandResult.Failure("api_error", "The AI proposed action could not be rejected.");
        }
    }

    private static string FailureCodeFor(ApiException ex) => ex.StatusCode == 403 ? "forbidden" : "api_error";
}
