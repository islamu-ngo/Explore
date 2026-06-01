// ABOUTME: Defines the Application-layer registry boundary for governed AI tool contracts.
// ABOUTME: Lets prompts, parsers, and future adapters consume one source of tool truth.

using Explore.Domain.Ai;

namespace Explore.Application.Features.AiAssistant.Tools;

public interface IAiToolContractRegistry
{
    IReadOnlyList<AiToolDefinition> Definitions { get; }

    AiToolDefinition? FindDefinition(AiProposedActionKind kind);

    AiToolValidationResult ValidatePayload(AiProposedActionKind kind, string payloadJson);
}
