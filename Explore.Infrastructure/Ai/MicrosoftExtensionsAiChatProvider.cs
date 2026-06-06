// ABOUTME: Adapts Microsoft.Extensions.AI IChatClient implementations to the platform AI provider contract.
// ABOUTME: Keeps SDK abstractions inside Infrastructure while preserving safe ATCR proposal and error semantics.

using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using Explore.Application.Contracts.Infrastructure.Ai;
using Explore.Application.Telemetry;
using Explore.Domain.Ai;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Ai;

public sealed class MicrosoftExtensionsAiChatProvider : IAiChatProvider, IAiModelCatalog
{
    public const string ProviderName = "microsoft-extensions-ai";

    private const string CreateEventDraftToolName = "create_event_draft";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IChatClient _chatClient;
    private readonly IOptions<AiProviderSettings> _options;
    private readonly BusinessMetrics _metrics;

    public MicrosoftExtensionsAiChatProvider(
        IChatClient chatClient,
        IOptions<AiProviderSettings> options,
        BusinessMetrics metrics)
    {
        _chatClient = chatClient;
        _options = options;
        _metrics = metrics;
    }

    public Task<IReadOnlyList<AiModelDescriptor>> ListAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var settings = _options.Value;

        if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.ModelId))
        {
            return Task.FromResult<IReadOnlyList<AiModelDescriptor>>([]);
        }

        IReadOnlyList<AiModelDescriptor> models =
        [
            new AiModelDescriptor(
                settings.ModelId.Trim(),
                settings.ModelId.Trim(),
                settings.MaxInputTokens,
                settings.MaxOutputTokens,
                SupportsToolProposals: true,
                SupportsStreaming: false)
        ];

        return Task.FromResult(models);
    }

    public async Task<AiChatProviderResult> SendAsync(AiChatPayload request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var settings = _options.Value;
        var providerName = ResolveTelemetryProvider(settings);
        var startedAt = Stopwatch.GetTimestamp();
        using var telemetryActivity = AiProviderTelemetry.StartRequest(providerName, request);

        if (!settings.Enabled)
        {
            return CompleteFailure(
                RecordFailure(providerName, "provider_disabled", "AI provider integration is disabled."),
                providerName,
                startedAt,
                telemetryActivity);
        }

        if (request.Options.StreamingEnabled)
        {
            return CompleteFailure(
                RecordFailure(providerName, "streaming_not_supported", "Streaming AI responses are not supported by this adapter yet."),
                providerName,
                startedAt,
                telemetryActivity);
        }

        if (request.Messages.Count == 0)
        {
            return CompleteFailure(
                RecordFailure(providerName, "empty_messages", "At least one message is required."),
                providerName,
                startedAt,
                telemetryActivity);
        }

        if (request.Messages.Any(message => message.Role == AiMessageRole.Tool))
        {
            return CompleteFailure(
                RecordFailure(providerName, "unsupported_message_role", "Tool result messages are not supported by this adapter yet."),
                providerName,
                startedAt,
                telemetryActivity);
        }

        if (!TryCreateChatOptions(request, providerName, out var chatOptions, out var optionsFailure))
        {
            return CompleteFailure(optionsFailure!, providerName, startedAt, telemetryActivity);
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(GetTimeout(settings, request.Options));

        try
        {
            var response = await _chatClient.GetResponseAsync(
                CreateMessages(request),
                chatOptions,
                timeoutCts.Token);

            if (!TryMapResponse(response, providerName, out var mappedResponse, out var responseFailure))
            {
                return CompleteFailure(responseFailure!, providerName, startedAt, telemetryActivity);
            }

            RecordSuccess(providerName, mappedResponse!, startedAt, telemetryActivity);
            return AiChatProviderResult.Success(mappedResponse!);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _metrics.RecordAiProviderRequestDuration(
                Stopwatch.GetElapsedTime(startedAt),
                providerName,
                "cancelled");
            AiProviderTelemetry.MarkCancelled(telemetryActivity);
            throw;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CompleteFailure(
                RecordFailure(providerName, "provider_timeout", "AI provider request timed out.", isTransient: true),
                providerName,
                startedAt,
                telemetryActivity);
        }
        catch (HttpRequestException)
        {
            return CompleteFailure(
                RecordFailure(providerName, "provider_unreachable", "AI provider request failed before a response was received.", isTransient: true),
                providerName,
                startedAt,
                telemetryActivity);
        }
        catch (JsonException)
        {
            return CompleteFailure(
                RecordFailure(providerName, "invalid_response", "AI provider returned an invalid response."),
                providerName,
                startedAt,
                telemetryActivity);
        }
        catch (InvalidOperationException)
        {
            return CompleteFailure(
                RecordFailure(providerName, "provider_failure", "AI provider request failed.", isTransient: true),
                providerName,
                startedAt,
                telemetryActivity);
        }
    }

    private static string ResolveTelemetryProvider(AiProviderSettings settings) =>
        settings.Provider.Equals(AiProviderSettings.ProviderOpenAiSdk, StringComparison.OrdinalIgnoreCase)
            ? AiProviderSettings.ProviderOpenAiSdk
            : settings.Provider.Equals(AiProviderSettings.ProviderAzureOpenAi, StringComparison.OrdinalIgnoreCase)
                ? AiProviderSettings.ProviderAzureOpenAi
                : ProviderName;

    private static TimeSpan GetTimeout(AiProviderSettings settings, AiChatOptions options)
    {
        var boundedSettingsTimeout = Math.Clamp(settings.TimeoutSeconds, 1, 300);
        var boundedRequestTimeout = Math.Clamp(options.TimeoutSeconds, 1, 300);
        return TimeSpan.FromSeconds(Math.Min(boundedSettingsTimeout, boundedRequestTimeout));
    }

    private static List<ChatMessage> CreateMessages(AiChatPayload request)
    {
        var messages = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            messages.Add(new ChatMessage(ChatRole.System, request.SystemPrompt.Trim()));
        }

        foreach (var message in request.Messages)
        {
            messages.Add(new ChatMessage(ToChatRole(message.Role), message.Content)
            {
                AuthorName = string.IsNullOrWhiteSpace(message.Name) ? null : message.Name.Trim()
            });
        }

        return messages;
    }

    private bool TryCreateChatOptions(
        AiChatPayload request,
        string providerName,
        out ChatOptions? chatOptions,
        out AiChatProviderResult? failure)
    {
        failure = null;
        chatOptions = new ChatOptions
        {
            ModelId = string.IsNullOrWhiteSpace(request.ModelId) ? null : request.ModelId.Trim(),
            Temperature = (float)request.Options.Temperature,
            MaxOutputTokens = request.Options.MaxOutputTokens,
            ToolMode = ChatToolMode.None
        };

        if (!request.Options.ToolProposalsEnabled || request.ActionSchema is null)
        {
            return true;
        }

        if (!TryCreateJsonSchema(request.ActionSchema.JsonSchema, providerName, out var schema, out failure))
        {
            return false;
        }

        var tools = new List<AITool>();
        foreach (var kind in request.ActionSchema.AllowedKinds.Distinct())
        {
            if (kind != AiProposedActionKind.CreateEventDraft)
            {
                continue;
            }

            tools.Add(new SchemaBackedFunctionDeclaration(
                CreateEventDraftToolName,
                "Propose a draft event payload. The platform persists this as a proposal that still requires explicit user confirmation.",
                schema!.Value));
        }

        if (tools.Count == 0)
        {
            return true;
        }

        chatOptions.Tools = tools;
        chatOptions.ToolMode = ChatToolMode.Auto;
        chatOptions.AllowMultipleToolCalls = true;
        return true;
    }

    private bool TryCreateJsonSchema(
        string schemaJson,
        string providerName,
        out JsonElement? schema,
        out AiChatProviderResult? failure)
    {
        schema = null;
        failure = null;

        try
        {
            using var document = JsonDocument.Parse(schemaJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                failure = RecordFailure(providerName, "invalid_action_schema", "AI action schema must be a JSON object.");
                return false;
            }

            schema = document.RootElement.Clone();
            return true;
        }
        catch (JsonException)
        {
            failure = RecordFailure(providerName, "invalid_action_schema", "AI action schema must be valid JSON.");
            return false;
        }
    }

    private bool TryMapResponse(
        ChatResponse providerResponse,
        string providerName,
        out AiChatResponse? chatResponse,
        out AiChatProviderResult? failure)
    {
        chatResponse = null;
        failure = null;

        if (providerResponse.FinishReason == ChatFinishReason.ContentFilter)
        {
            failure = RecordFailure(providerName, "content_filtered", "AI provider blocked the response because of content safety policy.");
            return false;
        }

        if (!TryMapProposedActions(providerResponse.Messages, providerName, out var proposedActions, out failure))
        {
            return false;
        }

        chatResponse = new AiChatResponse(
            providerResponse.Text,
            proposedActions,
            new AiTokenUsage(
                ToIntTokenCount(providerResponse.Usage?.InputTokenCount),
                ToIntTokenCount(providerResponse.Usage?.OutputTokenCount),
                ToIntTokenCount(providerResponse.Usage?.TotalTokenCount)),
            providerResponse.ResponseId,
            providerResponse.FinishReason?.Value);

        return true;
    }

    private bool TryMapProposedActions(
        IEnumerable<ChatMessage> messages,
        string providerName,
        out IReadOnlyList<AiProposedActionCandidate> proposedActions,
        out AiChatProviderResult? failure)
    {
        proposedActions = [];
        failure = null;

        var actions = new List<AiProposedActionCandidate>();
        foreach (var functionCall in messages.SelectMany(message => message.Contents).OfType<FunctionCallContent>())
        {
            if (!string.Equals(functionCall.Name, CreateEventDraftToolName, StringComparison.Ordinal))
            {
                continue;
            }

            if (functionCall.Exception is not null || !TryNormalizeToolArguments(functionCall.Arguments, providerName, out var payloadJson, out failure))
            {
                return false;
            }

            actions.Add(new AiProposedActionCandidate(
                AiProposedActionKind.CreateEventDraft,
                payloadJson!,
                "Create an event draft"));
        }

        proposedActions = actions;
        return true;
    }

    private bool TryNormalizeToolArguments(
        IDictionary<string, object?>? arguments,
        string providerName,
        out string? payloadJson,
        out AiChatProviderResult? failure)
    {
        payloadJson = null;
        failure = null;

        if (arguments is null || arguments.Count == 0)
        {
            failure = RecordFailure(providerName, "invalid_tool_arguments", "AI provider returned invalid action arguments.");
            return false;
        }

        using var document = JsonSerializer.SerializeToDocument(arguments, JsonOptions);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            failure = RecordFailure(providerName, "invalid_tool_arguments", "AI provider returned invalid action arguments.");
            return false;
        }

        payloadJson = document.RootElement.GetRawText();
        return true;
    }

    private static ChatRole ToChatRole(AiMessageRole role) => role switch
    {
        AiMessageRole.System => ChatRole.System,
        AiMessageRole.User => ChatRole.User,
        AiMessageRole.Assistant => ChatRole.Assistant,
        _ => ChatRole.User
    };

    private static int? ToIntTokenCount(long? value)
    {
        if (value is null || value < 0)
        {
            return null;
        }

        return value > int.MaxValue ? int.MaxValue : (int)value.Value;
    }

    private void RecordSuccess(
        string providerName,
        AiChatResponse response,
        long startedAt,
        Activity? telemetryActivity)
    {
        _metrics.RecordAiProviderRequest(providerName, "succeeded");
        _metrics.RecordAiProviderRequestDuration(
            Stopwatch.GetElapsedTime(startedAt),
            providerName,
            "succeeded");
        _metrics.RecordAiProviderTokenUsage(
            providerName,
            response.Usage.InputTokens,
            response.Usage.OutputTokens,
            response.Usage.TotalTokens);
        _metrics.RecordAiProviderProposedActions(
            providerName,
            response.ProposedActions.Count,
            "create_event_draft");
        AiProviderTelemetry.MarkSuccess(telemetryActivity, response);
    }

    private AiChatProviderResult CompleteFailure(
        AiChatProviderResult result,
        string providerName,
        long startedAt,
        Activity? telemetryActivity)
    {
        var error = result.Error!;
        _metrics.RecordAiProviderRequestDuration(
            Stopwatch.GetElapsedTime(startedAt),
            providerName,
            "failed",
            error.Code);
        AiProviderTelemetry.MarkFailure(telemetryActivity, error.Code, error.IsTransient);
        return result;
    }

    private AiChatProviderResult RecordFailure(string providerName, string code, string message, bool isTransient = false)
    {
        _metrics.RecordAiProviderRequest(providerName, "failed", code);
        return AiChatProviderResult.Failure(code, message, isTransient);
    }

    private sealed class SchemaBackedFunctionDeclaration : AIFunctionDeclaration
    {
        private readonly JsonElement _jsonSchema;

        public SchemaBackedFunctionDeclaration(string name, string description, JsonElement jsonSchema)
        {
            Name = name;
            Description = description;
            _jsonSchema = jsonSchema;
        }

        public override string Name { get; }

        public override string Description { get; }

        public override JsonElement JsonSchema => _jsonSchema;
    }
}
