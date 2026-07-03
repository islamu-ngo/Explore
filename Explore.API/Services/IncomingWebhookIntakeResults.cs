// ABOUTME: Result contracts for raw incoming webhook verification and idempotency capture.
// ABOUTME: Carries bounded ProblemDetails metadata without exposing raw payloads or secrets.

using Explore.API.ExceptionHandling;
using Explore.Application.Contracts.Webhooks;
using Microsoft.AspNetCore.Http;

namespace Explore.API.Services;

public sealed record IncomingWebhookReadResult(
    bool Succeeded,
    string Provider,
    string? RawPayload,
    string? PayloadHash,
    IReadOnlyDictionary<string, string> Headers,
    IncomingWebhookVerificationResult? Verification,
    int StatusCode,
    string Title,
    string Type,
    string Detail,
    string Code)
{
    public static IncomingWebhookReadResult Success(
        string provider,
        string rawPayload,
        string payloadHash,
        IReadOnlyDictionary<string, string> headers,
        IncomingWebhookVerificationResult verification) =>
        new(true, provider, rawPayload, payloadHash, headers, verification, StatusCodes.Status200OK, string.Empty, string.Empty, string.Empty, string.Empty);

    public static IncomingWebhookReadResult Failure(
        string provider,
        int statusCode,
        string title,
        string detail,
        string code) =>
        new(false, provider, null, null, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), null, statusCode, title, ProblemType(statusCode), detail, code);

    private static string ProblemType(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => ApiProblemTypes.BadRequest,
        StatusCodes.Status401Unauthorized => ApiProblemTypes.Unauthorized,
        StatusCodes.Status404NotFound => ApiProblemTypes.NotFound,
        StatusCodes.Status413PayloadTooLarge => ApiProblemTypes.PayloadTooLarge,
        StatusCodes.Status503ServiceUnavailable => ApiProblemTypes.ServiceUnavailable,
        _ => ApiProblemTypes.BadRequest
    };
}

public sealed record IncomingWebhookCaptureResult(
    bool Succeeded,
    bool IsDuplicate,
    Guid MessageId,
    string ProviderMessageId,
    string IdempotencyKey,
    int StatusCode,
    string Title,
    string Type,
    string Detail,
    string Code)
{
    public static IncomingWebhookCaptureResult Captured(
        Guid messageId,
        string providerMessageId,
        string idempotencyKey) =>
        new(true, false, messageId, providerMessageId, idempotencyKey, StatusCodes.Status200OK, string.Empty, string.Empty, string.Empty, string.Empty);

    public static IncomingWebhookCaptureResult Duplicate(
        Guid messageId,
        string providerMessageId,
        string idempotencyKey) =>
        new(true, true, messageId, providerMessageId, idempotencyKey, StatusCodes.Status200OK, string.Empty, string.Empty, string.Empty, string.Empty);

    public static IncomingWebhookCaptureResult Failure(
        int statusCode,
        string title,
        string detail,
        string code) =>
        new(false, false, Guid.Empty, string.Empty, string.Empty, statusCode, title, IncomingWebhookReadResult.Failure("incoming", statusCode, title, detail, code).Type, detail, code);
}
