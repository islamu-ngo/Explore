// ABOUTME: API DTO describing one data field in a canonical webhook event payload.
// ABOUTME: Keeps event-type catalog responses stable without exposing descriptor internals.

namespace Explore.Application.DTOs.Webhooks;

public sealed record WebhookEventDataFieldDto
{
    public required string Name { get; init; }

    public required string JsonType { get; init; }

    public required string Description { get; init; }

    public required string ExampleJson { get; init; }

    public bool Required { get; init; }
}
