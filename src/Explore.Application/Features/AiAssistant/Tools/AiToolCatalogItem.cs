// ABOUTME: Represents one scoped AI tool catalog item for assistant or MCP discovery.
// ABOUTME: Makes proposal availability explicit while never granting execution authority.

using Explore.Domain.Ai;

namespace Explore.Application.Features.AiAssistant.Tools;

public sealed record AiToolCatalogItem(
    AiProposedActionKind Kind,
    string Name,
    string DisplayName,
    AiToolAgentMetadata Metadata,
    bool CanRequestProposal,
    bool ExecutionAuthorityGranted,
    string AvailabilityCode,
    string AvailabilityReason);
