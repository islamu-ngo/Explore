// ABOUTME: MCP tool surface for safe discovery of AI tool contracts.
// ABOUTME: Exposes registry metadata only and never executes mutating tools directly.

using System.ComponentModel;
using System.Text.Json;
using Explore.Application.Features.AiAssistant.Tools;
using ModelContextProtocol.Server;

namespace Explore.API.Mcp;

[McpServerToolType]
public sealed class AiToolRegistryMcpTools(IAiToolContractRegistry registry)
{
    [McpServerTool(
        Name = "list_ai_tool_contracts",
        Title = "List AI tool contracts",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("List safe AI tool contracts exposed through the ISLAMU Event registry. Mutating tools still require proposal and confirmation.")]
    public string ListAiToolContracts(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var contracts = registry.Definitions
            .Where(definition => definition.ExposeToMcp)
            .Select(definition => new AiToolContractDescriptor(
                Kind: definition.Kind.ToString(),
                Name: definition.Name,
                DisplayName: definition.DisplayName,
                ConfirmationMode: definition.ConfirmationMode.ToString(),
                RequiredAuthorization: definition.RequiredAuthorization is null
                    ? null
                    : new AiToolAuthorizationDescriptor(
                        definition.RequiredAuthorization.ResourceKind,
                        definition.RequiredAuthorization.Action),
                AllowedPayloadFields: definition.AllowedPayloadFields.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
                ForbiddenPayloadFields: definition.ForbiddenPayloadFields.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
                JsonSchema: definition.JsonSchema))
            .ToArray();

        return JsonSerializer.Serialize(
            new AiToolContractListDescriptor(contracts),
            AiToolRegistryMcpJsonContext.Default.AiToolContractListDescriptor);
    }

    public sealed record AiToolContractListDescriptor(IReadOnlyList<AiToolContractDescriptor> Tools);

    public sealed record AiToolContractDescriptor(
        string Kind,
        string Name,
        string DisplayName,
        string ConfirmationMode,
        AiToolAuthorizationDescriptor? RequiredAuthorization,
        IReadOnlyList<string> AllowedPayloadFields,
        IReadOnlyList<string> ForbiddenPayloadFields,
        string JsonSchema);

    public sealed record AiToolAuthorizationDescriptor(string ResourceKind, string Action);
}
