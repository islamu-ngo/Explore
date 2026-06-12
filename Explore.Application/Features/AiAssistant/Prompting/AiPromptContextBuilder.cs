// ABOUTME: Builds bounded AI chat requests from persisted assistant conversation state.
// ABOUTME: Keeps provider prompt packing separate from send orchestration and persistence updates.

using Explore.Application.Contracts.Infrastructure.Ai;
using Explore.Application.Features.AiAssistant;
using Explore.Application.Settings.Groups;
using Explore.Domain.Ai;

namespace Explore.Application.Features.AiAssistant.Prompting;

public sealed class AiPromptContextBuilder
{
    private const int MaxProviderMessages = 32;

    private readonly AiSystemPromptFactory _systemPromptFactory;
    private readonly IAiTokenEstimator _tokenEstimator;

    public AiPromptContextBuilder()
        : this(new AiSystemPromptFactory())
    {
    }

    public AiPromptContextBuilder(
        AiSystemPromptFactory systemPromptFactory,
        IAiTokenEstimator? tokenEstimator = null)
    {
        _systemPromptFactory = systemPromptFactory;
        _tokenEstimator = tokenEstimator ?? new ApproximateAiTokenEstimator();
    }

    public AiChatPayload Build(
        AiConversation conversation,
        AiAssistantSettingGroup settings,
        string modelId,
        bool allowToolProposals = true)
    {
        var budget = AiPromptTokenBudget.Create(settings.MaxInputTokens);
        var toolProposalsEnabled = settings.ToolProposalsEnabled && allowToolProposals;
        string systemPrompt = _systemPromptFactory.CreateSystemPrompt(allowToolProposals);
        budget.ConsumeBestEffort(systemPrompt, _tokenEstimator);

        var actionSchema = _systemPromptFactory.CreateActionSchema(
            settings,
            toolProposalsEnabled,
            budget.RemainingTokens,
            _tokenEstimator);
        if (actionSchema is not null)
        {
            budget.ConsumeBestEffort(actionSchema.JsonSchema, _tokenEstimator);
        }

        var candidateMessages = conversation.Messages
            .OrderByDescending(message => message.Sequence)
            .Where(message => message.Role is AiMessageRole.System or AiMessageRole.User or AiMessageRole.Assistant)
            .Take(MaxProviderMessages)
            .ToList();
        var messages = PackMessages(candidateMessages, budget);

        return new AiChatPayload(
            modelId,
            messages,
            systemPrompt,
            new AiChatOptions(
                settings.MaxInputTokens,
                settings.MaxOutputTokens,
                settings.Temperature,
                AiAssistantAvailability.ResolveTimeoutSeconds(settings),
                toolProposalsEnabled,
                StreamingEnabled: false),
            actionSchema,
            ProviderConfiguration: BuildProviderConfiguration(settings, modelId));
    }

    private static AiChatProviderConfiguration BuildProviderConfiguration(
        AiAssistantSettingGroup settings,
        string modelId)
    {
        var provider = AiAssistantAvailability.NormalizeProvider(settings.Provider);
        return new AiChatProviderConfiguration(
            AiProviderDefaults.ProviderNameToId(provider),
            settings.EndpointUrl?.Trim() ?? string.Empty,
            settings.ApiKey?.Trim() ?? string.Empty,
            modelId.Trim());
    }

    private IReadOnlyList<AiChatMessage> PackMessages(
        IReadOnlyList<AiMessage> candidateMessages,
        AiPromptTokenBudget budget)
    {
        var selectedMessages = new List<AiChatMessage>();

        foreach (AiMessage message in candidateMessages)
        {
            string wrappedContent = WrapMessageContent(message.Role, message.Content);
            if (budget.TryConsume(wrappedContent, _tokenEstimator))
            {
                selectedMessages.Add(new AiChatMessage(message.Role, wrappedContent));
                continue;
            }

            if (selectedMessages.Count == 0)
            {
                string truncatedContent = CreateTruncatedWrappedMessage(message.Role, message.Content, budget.RemainingTokens);
                if (truncatedContent.Length > 0 && budget.TryConsume(truncatedContent, _tokenEstimator))
                {
                    selectedMessages.Add(new AiChatMessage(message.Role, truncatedContent));
                }
            }

            break;
        }

        selectedMessages.Reverse();
        return selectedMessages;
    }

    private string CreateTruncatedWrappedMessage(AiMessageRole role, string content, int maxTokens)
    {
        if (maxTokens <= 0)
        {
            return string.Empty;
        }

        string trimmed = content.Trim();
        var low = 0;
        var high = trimmed.Length;
        var best = string.Empty;

        while (low <= high)
        {
            int midpoint = low + ((high - low) / 2);
            string candidate = WrapMessageContent(role, trimmed[..midpoint]);
            if (_tokenEstimator.CountTokens(candidate) <= maxTokens)
            {
                best = candidate;
                low = midpoint + 1;
            }
            else
            {
                high = midpoint - 1;
            }
        }

        return best;
    }

    private static string WrapMessageContent(AiMessageRole role, string content)
        => $"<message role=\"{role}\">\n{content.Trim()}\n</message>";
}
