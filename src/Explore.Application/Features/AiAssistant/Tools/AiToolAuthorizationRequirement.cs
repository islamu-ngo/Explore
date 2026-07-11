// ABOUTME: Describes the authorization policy a governed AI tool must satisfy before execution.
// ABOUTME: Keeps future confirmation/executor flows aligned with existing Application resource actions.

namespace Explore.Application.Features.AiAssistant.Tools;

public sealed record AiToolAuthorizationRequirement(string ResourceKind, string Action);
