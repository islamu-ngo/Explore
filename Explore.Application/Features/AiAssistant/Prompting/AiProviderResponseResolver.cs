// ABOUTME: Resolves AI provider responses into validated proposed actions with one bounded correction retry.
// ABOUTME: Keeps provider retry policy separate from command persistence and domain state transitions.

using Explore.Application.Contracts.Infrastructure.Ai;
using Explore.Application.Features.AiAssistant.Tools;
using Explore.Domain.Ai;

namespace Explore.Application.Features.AiAssistant.Prompting;

public sealed class AiProviderResponseResolver
{
    private const string DefaultFailureCode = "invalid_tool_arguments";
    private const string DefaultFailureMessage = "AI provider returned invalid action payload JSON.";

    private readonly IAiChatProvider _chatProvider;
    private readonly AiStructuredActionParser _structuredActionParser;

    public AiProviderResponseResolver(
        IAiChatProvider chatProvider,
        AiStructuredActionParser structuredActionParser)
    {
        _chatProvider = chatProvider;
        _structuredActionParser = structuredActionParser;
    }

    public async Task<AiProviderResponseResolution> ResolveAsync(
        AiChatPayload providerPayload,
        CancellationToken cancellationToken)
    {
        var firstAttempt = await SendAndParseAsync(providerPayload, cancellationToken);
        if (firstAttempt.Succeeded || firstAttempt.ParseResult is null)
        {
            return firstAttempt;
        }

        var correctionPayload = providerPayload with
        {
            Messages = providerPayload.Messages
                .Append(new AiChatMessage(AiMessageRole.User, BuildSelfCorrectionMessage(firstAttempt.ParseResult)))
                .ToList()
        };

        return await SendAndParseAsync(correctionPayload, cancellationToken);
    }

    private async Task<AiProviderResponseResolution> SendAndParseAsync(
        AiChatPayload providerPayload,
        CancellationToken cancellationToken)
    {
        var providerResult = await _chatProvider.SendAsync(providerPayload, cancellationToken);
        if (!providerResult.Succeeded || providerResult.Response is null)
        {
            return AiProviderResponseResolution.Failure(
                providerResult.Error?.Code ?? "provider_failure",
                providerResult.Error?.Message ?? "AI provider failed.");
        }

        var parseResult = _structuredActionParser.Parse(providerResult.Response.ProposedActions);
        if (!parseResult.Succeeded)
        {
            return AiProviderResponseResolution.ParseFailure(providerResult.Response, parseResult);
        }

        return AiProviderResponseResolution.Success(providerResult.Response, parseResult);
    }

    private static string BuildSelfCorrectionMessage(AiStructuredActionParseResult parseResult)
    {
        var failureCode = string.IsNullOrWhiteSpace(parseResult.FailureCode)
            ? DefaultFailureCode
            : parseResult.FailureCode.Trim();
        var safeReason = string.IsNullOrWhiteSpace(parseResult.FailureMessage)
            ? DefaultFailureMessage
            : parseResult.FailureMessage.Trim();
        var correction = string.IsNullOrWhiteSpace(parseResult.CorrectionMessage)
            ? AiToolCorrectionMessages.SchemaExactRetry
            : parseResult.CorrectionMessage.Trim();

        return $"""
            The previous proposed tool call was rejected by platform validation.
            Failure code: {failureCode}
            Safe reason: {safeReason}

            {correction}
            Return either no tool call, or retry with corrected tool call arguments only. Do not reveal validation internals or include raw rejected arguments.
            """;
    }
}

public sealed record AiProviderResponseResolution(
    bool Succeeded,
    AiChatResponse? Response,
    AiStructuredActionParseResult? ParseResult,
    string? FailureCode,
    string? FailureMessage)
{
    private const string DefaultFailureCode = "invalid_tool_arguments";
    private const string DefaultFailureMessage = "AI provider returned invalid action payload JSON.";

    public static AiProviderResponseResolution Success(
        AiChatResponse response,
        AiStructuredActionParseResult parseResult)
        => new(true, response, parseResult, null, null);

    public static AiProviderResponseResolution Failure(string failureCode, string failureMessage)
        => new(false, null, null, failureCode, failureMessage);

    public static AiProviderResponseResolution ParseFailure(
        AiChatResponse response,
        AiStructuredActionParseResult parseResult)
        => new(
            false,
            response,
            parseResult,
            parseResult.FailureCode ?? DefaultFailureCode,
            parseResult.FailureMessage ?? DefaultFailureMessage);
}
