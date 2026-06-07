// ABOUTME: Generates deterministic fake/replay AI usability reports for assistant and MCP flows.
// ABOUTME: Exercises registry, HAL catalog, plan validation, and recovery without live providers or persistence writes.

using Explore.Application.Features.AiAssistant.Plans;
using Explore.Application.Features.AiAssistant.Tools;
using Explore.Domain.Ai;

namespace Explore.Diagnostic.AiReplay;

public sealed class AiReplayReportGenerator
{
    private const string Version = "atcr-ai-replay-v1";
    private const string RequiredCreateEventHalRel = "create-event";
    private const string CreateEventDraftToolName = "CreateEventDraft";
    private const string ProjectedCreateEventDraftMcpToolName = "propose_create_event_draft";
    private const string ValidFixturePayloadJson = "{\"title\":\"Replay fixture\"}";
    private const string MissingTitleFixturePayloadJson = "{\"description\":\"Replay fixture missing a required field\"}";

    private static readonly Guid TenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000011");
    private static readonly Guid ConversationId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000012");
    private static readonly DateTime UtcNow = new(2026, 6, 7, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan MaxContextAge = TimeSpan.FromMinutes(30);

    private readonly Func<DateTimeOffset> _clock;
    private readonly IAiToolContractRegistry _registry;
    private readonly AiToolCatalogService _catalogService;
    private readonly AiProposedPlanValidator _planValidator;

    public AiReplayReportGenerator(Func<DateTimeOffset>? clock = null, IAiToolContractRegistry? registry = null)
    {
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _registry = registry ?? AiToolContractRegistry.CreateDefault();
        _catalogService = new AiToolCatalogService(_registry);
        _planValidator = new AiProposedPlanValidator(_registry);
    }

    public AiReplayReport Generate()
    {
        var results = new[]
        {
            ReplayAssistantRailProposalPreview(),
            ReplayMcpInspectorContract(),
            ReplayMcpProposalFirstFlow(),
            ReplayMcpProjectedToolSelection(),
            ReplayMcpConfirmationRequired(),
            ReplayMissingHalBlocksPreview(),
            ReplayInvalidPayloadRecovery(),
        };

        var report = new AiReplayReport(
            _clock(),
            Version,
            UsesLiveProviderCredentials: false,
            ContainsContentBearingArtifacts: false,
            results);

        return report with
        {
            ContainsContentBearingArtifacts = AiReplayArtifactSafetyPolicy.ContainsContentBearingData(report)
        };
    }

    private AiReplayScenarioResult ReplayAssistantRailProposalPreview()
    {
        var catalog = _catalogService.GetCatalog(CreateCatalogQuery(HalSet(RequiredCreateEventHalRel)));
        var planResult = _planValidator.Validate(CreatePlan(), CreatePlanContext(HalSet(RequiredCreateEventHalRel)));

        if (catalog.SingleOrDefault()?.CanRequestProposal == true &&
            planResult.CanRequestConfirmation &&
            !planResult.ExecutionAuthorityGranted &&
            planResult.Steps.All(step => !step.ExecutionAuthorityGranted))
        {
            return AiReplayScenarioResult.Pass(
                AiReplayScenarioCodes.AssistantRailProposalPreview,
                "Assistant rail replay produced a confirmation-ready proposal preview without execution authority.",
                "Catalog HAL gate, plan freshness, schema validation, and confirmation-only posture passed.");
        }

        return AiReplayScenarioResult.Fail(
            AiReplayScenarioCodes.AssistantRailProposalPreview,
            AiReplayFailureClass.ProposalValidation,
            "Assistant rail replay did not produce a safe proposal preview.",
            "Review scoped catalog inputs, HAL rels, and plan validator readiness checks.");
    }

    private AiReplayScenarioResult ReplayMcpInspectorContract()
    {
        var definition = _registry.FindDefinition(AiProposedActionKind.CreateEventDraft);

        if (definition is not null &&
            definition.ExposeToMcp &&
            definition.Name == CreateEventDraftToolName &&
            definition.ConfirmationMode == AiToolConfirmationMode.Required)
        {
            return AiReplayScenarioResult.Pass(
                AiReplayScenarioCodes.McpInspectorContract,
                "MCP Inspector replay has a bounded discovery checklist for tools, resources, prompts, and proposal-only calls.",
                $"Expected smoke scope: tools/list, resources/list, prompts/list, list_ai_tool_contracts, {ProjectedCreateEventDraftMcpToolName}, ai_conversations, ai_conversation_detail, and create_event_draft_with_confirmation; do not confirm or execute proposed actions.");
        }

        return AiReplayScenarioResult.Fail(
            AiReplayScenarioCodes.McpInspectorContract,
            AiReplayFailureClass.CatalogAuthorization,
            "MCP Inspector replay could not derive the expected registry-backed discovery checklist.",
            "Review registry MCP exposure and confirmation mode before running manual Inspector smoke checks.");
    }

    private AiReplayScenarioResult ReplayMcpProposalFirstFlow()
    {
        var definition = _registry.FindDefinition(AiProposedActionKind.CreateEventDraft);
        var validation = _registry.ValidatePayload(AiProposedActionKind.CreateEventDraft, ValidFixturePayloadJson);

        if (definition is not null &&
            definition.ExposeToMcp &&
            definition.ConfirmationMode == AiToolConfirmationMode.Required &&
            validation.Succeeded)
        {
            return AiReplayScenarioResult.Pass(
                AiReplayScenarioCodes.McpProposalFirst,
                "MCP replay validated tool arguments and stopped at proposed-action confirmation.",
                "Registry validation passed and no executor, repository, or MediatR command was invoked by the replay harness.");
        }

        return AiReplayScenarioResult.Fail(
            AiReplayScenarioCodes.McpProposalFirst,
            AiReplayFailureClass.SideEffectSafety,
            "MCP replay did not preserve proposal-first behavior.",
            "Review registry MCP exposure, confirmation mode, and proposal command wiring.");
    }

    private AiReplayScenarioResult ReplayMcpProjectedToolSelection()
    {
        var definition = _registry.FindDefinition(AiProposedActionKind.CreateEventDraft);

        if (definition is not null &&
            definition.ExposeToMcp &&
            definition.AllowedPayloadFields.Contains("title") &&
            definition.ForbiddenPayloadFields.Contains("tenantId") &&
            ProjectedCreateEventDraftMcpToolName.StartsWith("propose_", StringComparison.Ordinal))
        {
            return AiReplayScenarioResult.Pass(
                AiReplayScenarioCodes.McpProjectedToolSelection,
                "MCP replay selects the projected proposal tool for event drafts instead of inventing privileged fields.",
                $"Use {ProjectedCreateEventDraftMcpToolName} with conversationId plus allow-listed draft fields; hidden runtime fields and confirmation endpoints remain out of scope for agents.");
        }

        return AiReplayScenarioResult.Fail(
            AiReplayScenarioCodes.McpProjectedToolSelection,
            AiReplayFailureClass.ProposalValidation,
            "MCP replay could not prove safe projected-tool selection.",
            "Review registry allowed/forbidden fields and projected MCP tool naming before client smoke.");
    }

    private AiReplayScenarioResult ReplayMcpConfirmationRequired()
    {
        var definition = _registry.FindDefinition(AiProposedActionKind.CreateEventDraft);
        var instructions = definition?.EffectiveAgentMetadata.SafeActionInstructions ?? string.Empty;

        if (definition is not null &&
            definition.ConfirmationMode == AiToolConfirmationMode.Required &&
            definition.EffectiveAgentMetadata.ApprovalMode == AiToolApprovalMode.HumanConfirmationRequired &&
            instructions.Contains("proposal", StringComparison.OrdinalIgnoreCase) &&
            instructions.Contains("confirms", StringComparison.OrdinalIgnoreCase))
        {
            return AiReplayScenarioResult.Pass(
                AiReplayScenarioCodes.McpConfirmationRequired,
                "MCP replay states that event-draft calls create proposals only and require product confirmation before side effects.",
                "Agent-facing guidance preserves proposal-vs-execution language and does not claim that an event exists before confirmation.");
        }

        return AiReplayScenarioResult.Fail(
            AiReplayScenarioCodes.McpConfirmationRequired,
            AiReplayFailureClass.SideEffectSafety,
            "MCP replay could not verify confirmation-required guidance.",
            "Review registry confirmation mode, approval metadata, and MCP prompt/runbook wording.");
    }

    private AiReplayScenarioResult ReplayMissingHalBlocksPreview()
    {
        var catalog = _catalogService.GetCatalog(CreateCatalogQuery(HalSet()));
        var planResult = _planValidator.Validate(CreatePlan(), CreatePlanContext(HalSet()));

        if (catalog.SingleOrDefault()?.CanRequestProposal == false &&
            !planResult.CanRequestConfirmation &&
            planResult.FailureCode == AiPlanValidationFailureCodes.MissingHalAffordance &&
            !planResult.ExecutionAuthorityGranted)
        {
            return AiReplayScenarioResult.Pass(
                AiReplayScenarioCodes.AssistantRailMissingHal,
                "Missing HAL replay blocked proposal confirmation without granting execution authority.",
                "HAL affordance absence remained the source of truth for mutating affordance visibility.");
        }

        return AiReplayScenarioResult.Fail(
            AiReplayScenarioCodes.AssistantRailMissingHal,
            AiReplayFailureClass.CatalogAuthorization,
            "Missing HAL replay did not block the mutating proposal path.",
            "Review route/workflow catalog and plan-preview HAL checks.");
    }

    private AiReplayScenarioResult ReplayInvalidPayloadRecovery()
    {
        var plan = CreatePlan(MissingTitleFixturePayloadJson);
        var planResult = _planValidator.Validate(plan, CreatePlanContext(HalSet(RequiredCreateEventHalRel)));
        var step = planResult.Steps.SingleOrDefault();

        if (!planResult.CanRequestConfirmation &&
            planResult.FailureCode == "missing_tool_argument" &&
            step?.Status == AiPlanStepStatus.RequiresClarification &&
            step.NextActions.Count > 0)
        {
            return AiReplayScenarioResult.Pass(
                AiReplayScenarioCodes.InvalidPayloadRecovery,
                "Invalid payload replay returned structured clarification recovery without echoing fixture content.",
                "Stable recovery code and next action were available for UI/MCP retry guidance.");
        }

        return AiReplayScenarioResult.Fail(
            AiReplayScenarioCodes.InvalidPayloadRecovery,
            AiReplayFailureClass.Recovery,
            "Invalid payload replay did not produce safe recovery metadata.",
            "Review payload guard recovery metadata and plan validator recovery propagation.");
    }

    private static AiToolCatalogQuery CreateCatalogQuery(IReadOnlySet<string> halRels)
        => new(
            TenantId,
            IsAuthenticated: true,
            AiToolCatalogPrincipalKind.User,
            RoutePath: "/events",
            WorkflowScope: "event-drafting",
            ContextScope: "selected-references",
            halRels);

    private static AiPlanValidationContext CreatePlanContext(IReadOnlySet<string> halRels)
        => new(TenantId, halRels, UtcNow, MaxContextAge);

    private static AiProposedPlan CreatePlan(string payloadJson = ValidFixturePayloadJson)
        => new(
            TenantId,
            ConversationId,
            [
                new AiProposedPlanStep(
                    "step-1",
                    AiProposedActionKind.CreateEventDraft,
                    CreateEventDraftToolName,
                    payloadJson,
                    UtcNow)
            ]);

    private static HashSet<string> HalSet(params string[] rels) => new(rels, StringComparer.OrdinalIgnoreCase);
}
