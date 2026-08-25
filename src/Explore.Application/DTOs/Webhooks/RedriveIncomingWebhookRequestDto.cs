// ABOUTME: API request body for redriving a dead-lettered incoming webhook generation.
// ABOUTME: Carries optimistic-generation evidence and a bounded operator reason without tenant authority.

namespace Explore.Application.DTOs.Webhooks;

public sealed record RedriveIncomingWebhookRequestDto
{
    public int ExpectedProcessingGeneration { get; init; }

    public string Reason { get; init; } = string.Empty;
}
