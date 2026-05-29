// ABOUTME: Safe health snapshot contract for AI provider readiness reporting.
// ABOUTME: Exposes bounded provider state without secrets, endpoint URLs, prompts, or model content.

namespace Explore.Application.Contracts.Infrastructure.Ai;

public sealed record AiProviderHealth(
    bool Enabled,
    bool Healthy,
    string Status,
    string Description,
    IReadOnlyDictionary<string, object> Data);
