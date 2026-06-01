// ABOUTME: Implements the OpenAI-compatible chat provider adapter using raw HTTP and platform contracts.
// ABOUTME: Maps chat completions and tool calls into safe provider-neutral results without SDK leakage.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Explore.Application.Contracts.Infrastructure.Ai;
using Explore.Application.Telemetry;
using Explore.Domain.Ai;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Ai;

public sealed class OpenAiCompatibleChatProvider : IAiChatProvider, IAiModelCatalog
{
    public const string HttpClientName = "OpenAiCompatibleAiClient";

    private const string CreateEventDraftToolName = "create_event_draft";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<AiProviderSettings> _options;
    private readonly AiProviderSettingsValidator _validator;
    private readonly BusinessMetrics _metrics;

    public OpenAiCompatibleChatProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<AiProviderSettings> options,
        AiProviderSettingsValidator validator,
        BusinessMetrics metrics)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _validator = validator;
        _metrics = metrics;
    }

    public Task<IReadOnlyList<AiModelDescriptor>> ListAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var settings = _options.Value;

        if (!IsRunnableOpenAiCompatible(settings) || !ValidateSettings(settings).Succeeded)
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

        if (!IsRunnableOpenAiCompatible(settings))
        {
            return RecordFailure("provider_not_configured", "OpenAI-compatible provider is not enabled or configured.");
        }

        var validation = ValidateSettings(settings);
        if (!validation.Succeeded)
        {
            return RecordFailure("invalid_settings", "AI provider settings are invalid.");
        }

        if (request.Options.StreamingEnabled)
        {
            return RecordFailure("streaming_not_supported", "Streaming AI responses are not supported by this adapter yet.");
        }

        if (request.Messages.Count == 0)
        {
            return RecordFailure("empty_messages", "At least one message is required.");
        }

        if (request.Messages.Any(message => message.Role == AiMessageRole.Tool))
        {
            return RecordFailure("unsupported_message_role", "Tool result messages are not supported by this adapter yet.");
        }

        if (!TryCreatePayload(settings, request, out var payload, out var payloadFailure))
        {
            return payloadFailure!;
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(GetTimeout(settings, request.Options));

        try
        {
            using var httpRequest = CreateHttpRequest(settings, payload!);
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                return RecordHttpFailure(response.StatusCode);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(timeoutCts.Token);
            var providerResponse = await JsonSerializer.DeserializeAsync<OpenAiChatCompletionResponse>(stream, JsonOptions, timeoutCts.Token);

            if (providerResponse is null || providerResponse.Choices.Count == 0)
            {
                return RecordFailure("invalid_response", "AI provider returned an invalid response.");
            }

            if (!TryMapResponse(providerResponse, response, out var chatResponse, out var responseFailure))
            {
                return responseFailure!;
            }

            _metrics.RecordAiProviderRequest(AiProviderSettings.ProviderOpenAiCompatible, "succeeded");
            return AiChatProviderResult.Success(chatResponse!);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return RecordFailure("provider_timeout", "AI provider request timed out.", isTransient: true);
        }
        catch (HttpRequestException)
        {
            return RecordFailure("provider_unreachable", "AI provider request failed before a response was received.", isTransient: true);
        }
        catch (JsonException)
        {
            return RecordFailure("invalid_response", "AI provider returned an invalid response.");
        }
    }

    private static bool IsRunnableOpenAiCompatible(AiProviderSettings settings) =>
        settings.Enabled
        && settings.Provider.Equals(AiProviderSettings.ProviderOpenAiCompatible, StringComparison.OrdinalIgnoreCase);

    private Microsoft.Extensions.Options.ValidateOptionsResult ValidateSettings(AiProviderSettings settings) =>
        _validator.Validate(null, settings);

    private static TimeSpan GetTimeout(AiProviderSettings settings, AiChatOptions options)
    {
        var boundedSettingsTimeout = Math.Clamp(settings.TimeoutSeconds, 1, 300);
        var boundedRequestTimeout = Math.Clamp(options.TimeoutSeconds, 1, 300);
        return TimeSpan.FromSeconds(Math.Min(boundedSettingsTimeout, boundedRequestTimeout));
    }

    private static HttpRequestMessage CreateHttpRequest(AiProviderSettings settings, OpenAiChatCompletionRequest payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, BuildChatCompletionsUri(settings.EndpointUrl));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        return request;
    }

    private static Uri BuildChatCompletionsUri(string endpointUrl)
    {
        var endpoint = new Uri(endpointUrl, UriKind.Absolute);
        var path = endpoint.AbsolutePath.TrimEnd('/');

        if (path.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
        {
            return endpoint;
        }

        var builder = new UriBuilder(endpoint)
        {
            Path = string.IsNullOrEmpty(path)
                ? "chat/completions"
                : $"{path}/chat/completions"
        };

        return builder.Uri;
    }

    private bool TryCreatePayload(
        AiProviderSettings settings,
        AiChatPayload request,
        out OpenAiChatCompletionRequest? payload,
        out AiChatProviderResult? failure)
    {
        payload = null;
        failure = null;

        var tools = new List<OpenAiTool>();
        if (request.Options.ToolProposalsEnabled && request.ActionSchema is not null)
        {
            foreach (var kind in request.ActionSchema.AllowedKinds.Distinct())
            {
                if (kind != AiProposedActionKind.CreateEventDraft)
                {
                    continue;
                }

                if (!TryCreateJsonSchema(request.ActionSchema.JsonSchema, out var schema, out failure))
                {
                    return false;
                }

                tools.Add(new OpenAiTool(
                    "function",
                    new OpenAiFunctionTool(
                        CreateEventDraftToolName,
                        "Propose a draft event payload. The platform persists this as a proposal that still requires explicit user confirmation.",
                        schema!.Value)));
            }
        }

        var messages = new List<OpenAiMessage>();
        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            messages.Add(new OpenAiMessage("system", request.SystemPrompt.Trim(), null));
        }

        messages.AddRange(request.Messages.Select(message => new OpenAiMessage(
            ToOpenAiRole(message.Role),
            message.Content,
            string.IsNullOrWhiteSpace(message.Name) ? null : message.Name.Trim())));

        payload = new OpenAiChatCompletionRequest
        {
            Model = string.IsNullOrWhiteSpace(request.ModelId) ? settings.ModelId.Trim() : request.ModelId.Trim(),
            Messages = messages,
            Temperature = request.Options.Temperature,
            MaxTokens = request.Options.MaxOutputTokens,
            Stream = false,
            Tools = tools.Count == 0 ? null : tools,
            ToolChoice = tools.Count == 0 ? null : "auto"
        };

        return true;
    }

    private bool TryCreateJsonSchema(string schemaJson, out JsonElement? schema, out AiChatProviderResult? failure)
    {
        schema = null;
        failure = null;

        try
        {
            using var document = JsonDocument.Parse(schemaJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                failure = RecordFailure("invalid_action_schema", "AI action schema must be a JSON object.");
                return false;
            }

            schema = document.RootElement.Clone();
            return true;
        }
        catch (JsonException)
        {
            failure = RecordFailure("invalid_action_schema", "AI action schema must be valid JSON.");
            return false;
        }
    }

    private bool TryMapResponse(
        OpenAiChatCompletionResponse providerResponse,
        HttpResponseMessage response,
        out AiChatResponse? chatResponse,
        out AiChatProviderResult? failure)
    {
        chatResponse = null;
        failure = null;

        var choice = providerResponse.Choices[0];
        if (choice.Message is null)
        {
            failure = RecordFailure("invalid_response", "AI provider returned an invalid response.");
            return false;
        }

        if (!TryMapProposedActions(choice.Message.ToolCalls, out var proposedActions, out failure))
        {
            return false;
        }

        chatResponse = new AiChatResponse(
            choice.Message.Content ?? string.Empty,
            proposedActions,
            new AiTokenUsage(
                providerResponse.Usage?.PromptTokens,
                providerResponse.Usage?.CompletionTokens,
                providerResponse.Usage?.TotalTokens),
            GetProviderRequestId(response),
            choice.FinishReason);

        return true;
    }

    private bool TryMapProposedActions(
        IReadOnlyList<OpenAiToolCall>? toolCalls,
        out IReadOnlyList<AiProposedActionCandidate> proposedActions,
        out AiChatProviderResult? failure)
    {
        proposedActions = [];
        failure = null;

        if (toolCalls is null || toolCalls.Count == 0)
        {
            return true;
        }

        var actions = new List<AiProposedActionCandidate>();
        foreach (var toolCall in toolCalls)
        {
            if (!string.Equals(toolCall.Type, "function", StringComparison.OrdinalIgnoreCase)
                || toolCall.Function is null
                || !string.Equals(toolCall.Function.Name, CreateEventDraftToolName, StringComparison.Ordinal))
            {
                continue;
            }

            if (!TryNormalizeToolArguments(toolCall.Function.Arguments, out var payloadJson, out failure))
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

    private bool TryNormalizeToolArguments(string? arguments, out string? payloadJson, out AiChatProviderResult? failure)
    {
        payloadJson = null;
        failure = null;

        if (string.IsNullOrWhiteSpace(arguments))
        {
            failure = RecordFailure("invalid_tool_arguments", "AI provider returned invalid action arguments.");
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(arguments);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                failure = RecordFailure("invalid_tool_arguments", "AI provider returned invalid action arguments.");
                return false;
            }

            payloadJson = document.RootElement.GetRawText();
            return true;
        }
        catch (JsonException)
        {
            failure = RecordFailure("invalid_tool_arguments", "AI provider returned invalid action arguments.");
            return false;
        }
    }

    private static string ToOpenAiRole(AiMessageRole role) => role switch
    {
        AiMessageRole.System => "system",
        AiMessageRole.User => "user",
        AiMessageRole.Assistant => "assistant",
        _ => "user"
    };

    private static string? GetProviderRequestId(HttpResponseMessage response)
    {
        string[] headerNames = ["x-request-id", "openai-request-id", "request-id"];
        foreach (var headerName in headerNames)
        {
            if (response.Headers.TryGetValues(headerName, out var values))
            {
                return values.FirstOrDefault();
            }
        }

        return null;
    }

    private AiChatProviderResult RecordHttpFailure(HttpStatusCode statusCode)
    {
        var category = $"http_{(int)statusCode}";
        var isTransient = statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests
            || (int)statusCode >= 500;

        return RecordFailure(
            category,
            $"AI provider request failed with HTTP status {(int)statusCode}.",
            isTransient);
    }

    private AiChatProviderResult RecordFailure(string code, string message, bool isTransient = false)
    {
        _metrics.RecordAiProviderRequest(AiProviderSettings.ProviderOpenAiCompatible, "failed", code);
        return AiChatProviderResult.Failure(code, message, isTransient);
    }

    private sealed record OpenAiChatCompletionRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; init; } = string.Empty;

        [JsonPropertyName("messages")]
        public IReadOnlyList<OpenAiMessage> Messages { get; init; } = [];

        [JsonPropertyName("temperature")]
        public decimal Temperature { get; init; }

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; init; }

        [JsonPropertyName("stream")]
        public bool Stream { get; init; }

        [JsonPropertyName("tools")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<OpenAiTool>? Tools { get; init; }

        [JsonPropertyName("tool_choice")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ToolChoice { get; init; }
    }

    private sealed record OpenAiMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content,
        [property: JsonPropertyName("name")] [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Name);

    private sealed record OpenAiTool(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("function")] OpenAiFunctionTool Function);

    private sealed record OpenAiFunctionTool(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("parameters")] JsonElement Parameters);

    private sealed record OpenAiChatCompletionResponse(
        [property: JsonPropertyName("choices")] IReadOnlyList<OpenAiChoice> Choices,
        [property: JsonPropertyName("usage")] OpenAiUsage? Usage);

    private sealed record OpenAiChoice(
        [property: JsonPropertyName("message")] OpenAiResponseMessage? Message,
        [property: JsonPropertyName("finish_reason")] string? FinishReason);

    private sealed record OpenAiResponseMessage(
        [property: JsonPropertyName("content")] string? Content,
        [property: JsonPropertyName("tool_calls")] IReadOnlyList<OpenAiToolCall>? ToolCalls);

    private sealed record OpenAiToolCall(
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("function")] OpenAiFunctionCall? Function);

    private sealed record OpenAiFunctionCall(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("arguments")] string? Arguments);

    private sealed record OpenAiUsage(
        [property: JsonPropertyName("prompt_tokens")] int? PromptTokens,
        [property: JsonPropertyName("completion_tokens")] int? CompletionTokens,
        [property: JsonPropertyName("total_tokens")] int? TotalTokens);
}
