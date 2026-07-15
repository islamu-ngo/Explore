// ABOUTME: Application result for validated webhook bulk replay preview requests.
// ABOUTME: Separates public preview DTOs from safe validation failure codes and messages.

using Explore.Application.DTOs.Webhooks;

namespace Explore.Application.Responses;

public sealed record WebhookBulkReplayPreviewResult(
    WebhookBulkReplayPreviewDto? Preview,
    string? FailureCode = null,
    IReadOnlyList<string>? Errors = null)
{
    public bool Success => Preview is not null;

    public static WebhookBulkReplayPreviewResult Succeeded(WebhookBulkReplayPreviewDto preview) =>
        new(preview);

    public static WebhookBulkReplayPreviewResult Failed(string failureCode, IEnumerable<string> errors) =>
        new(null, failureCode, errors.ToArray());
}
