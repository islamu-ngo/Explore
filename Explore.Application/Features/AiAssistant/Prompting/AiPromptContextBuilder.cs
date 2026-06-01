// ABOUTME: Builds bounded AI chat requests from persisted assistant conversation state.
// ABOUTME: Keeps provider prompt packing separate from send orchestration and persistence updates.

using Explore.Application.Contracts.Infrastructure.Ai;
using Explore.Application.Settings.Groups;
using Explore.Domain.Ai;

namespace Explore.Application.Features.AiAssistant.Prompting;

public sealed class AiPromptContextBuilder
{
    private const int MaxProviderMessages = 32;

    private readonly AiSystemPromptFactory _systemPromptFactory;

    public AiPromptContextBuilder()
        : this(new AiSystemPromptFactory())
    {
    }

    public AiPromptContextBuilder(AiSystemPromptFactory systemPromptFactory)
    {
        _systemPromptFactory = systemPromptFactory;
    }

    public AiChatPayload Build(
        AiConversation conversation,
        AiAssistantSettingGroup settings,
        string modelId)
    {
        var messages = conversation.Messages
            .OrderByDescending(message => message.Sequence)
            .Where(message => message.Role is AiMessageRole.System or AiMessageRole.User or AiMessageRole.Assistant)
            .Take(MaxProviderMessages)
            .OrderBy(message => message.Sequence)
            .Select(message => new AiChatMessage(message.Role, WrapMessageContent(message.Role, message.Content)))
            .ToList();

        return new AiChatPayload(
            modelId,
            messages,
            _systemPromptFactory.CreateSystemPrompt(),
            new AiChatOptions(
                settings.MaxInputTokens,
                settings.MaxOutputTokens,
                settings.Temperature,
                settings.TimeoutSeconds,
                settings.ToolProposalsEnabled,
                StreamingEnabled: false),
            _systemPromptFactory.CreateActionSchema(settings));
    }

    private static string WrapMessageContent(AiMessageRole role, string content)
        => $"<message role=\"{role}\">\n{content.Trim()}\n</message>";
}
