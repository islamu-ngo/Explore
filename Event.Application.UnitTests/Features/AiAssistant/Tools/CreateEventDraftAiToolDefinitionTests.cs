// ABOUTME: Unit tests for the registry-backed CreateEventDraft AI tool definition.
// ABOUTME: Locks schema, mapper, authorization, and payload-field metadata against silent drift.

using System.Text.Json;
using Explore.Application.Authorization;
using Explore.Application.Features.AiAssistant.Actions;
using Explore.Application.Features.AiAssistant.Tools;
using Explore.Domain.Ai;

namespace Event.Application.UnitTests.Features.AiAssistant.Tools;

public sealed class CreateEventDraftAiToolDefinitionTests
{
    [Test]
    public async Task Create_ReturnsRequiredGovernanceMetadata()
    {
        var definition = CreateEventDraftAiToolDefinition.Create();

        await Assert.That(definition.Kind).IsEqualTo(AiProposedActionKind.CreateEventDraft);
        await Assert.That(definition.Name).IsEqualTo("CreateEventDraft");
        await Assert.That(definition.ConfirmationMode).IsEqualTo(AiToolConfirmationMode.Required);
        await Assert.That(definition.PayloadMapperType).IsEqualTo(typeof(CreateEventDraftAiActionMapper));
        await Assert.That(definition.RequiredAuthorization).IsNotNull();
        await Assert.That(definition.RequiredAuthorization!.ResourceKind).IsEqualTo(ResourceKinds.Event);
        await Assert.That(definition.RequiredAuthorization.Action).IsEqualTo(AuthorizationActions.Create);
        await Assert.That(definition.ExposeToProvider).IsTrue();
        await Assert.That(definition.ExposeToMcp).IsTrue();
        await Assert.That(definition.EffectiveAgentMetadata.RiskClass).IsEqualTo(AiToolRiskClass.Medium);
        await Assert.That(definition.EffectiveAgentMetadata.ApprovalMode).IsEqualTo(AiToolApprovalMode.HumanConfirmationRequired);
    }

    [Test]
    public async Task Create_ReturnsAgentUxMetadataWithoutExecutionAuthority()
    {
        var metadata = CreateEventDraftAiToolDefinition.Create().EffectiveAgentMetadata;

        await Assert.That(metadata.Scopes.RouteScopes).Contains("/events");
        await Assert.That(metadata.Scopes.WorkflowScopes).Contains("event-drafting");
        await Assert.That(metadata.Scopes.ContextScopes).Contains("selected-references");
        await Assert.That(metadata.AvailabilityReason).Contains("API/HAL");
        await Assert.That(metadata.FollowUpPolicy).IsEqualTo(AiToolFollowUpPolicy.AskClarifyingQuestionBeforeProposal);
        await Assert.That(metadata.SafeActionInstructions).Contains("draft proposal only");
        await Assert.That(metadata.ResultPresentation.CardKind).IsEqualTo("event-draft-proposal-card");
        await Assert.That(metadata.RequiredHalLinkRel).IsEqualTo("create-event");
    }

    [Test]
    public async Task EffectiveAgentMetadata_WhenDefinitionOmitsMetadata_ReturnsSafeDefault()
    {
        var definition = new AiToolDefinition(
            AiProposedActionKind.CreateEventDraft,
            "CreateEventDraft",
            "Create event draft",
            "{}",
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        await Assert.That(definition.EffectiveAgentMetadata.ApprovalMode).IsEqualTo(AiToolApprovalMode.HumanConfirmationRequired);
        await Assert.That(definition.EffectiveAgentMetadata.AvailabilityReason).Contains("API/HAL");
    }

    [Test]
    public async Task JsonSchemaProperties_MatchAllowedPayloadFields()
    {
        var schemaFields = GetSchemaPropertyNames();

        await Assert.That(schemaFields).IsEquivalentTo(CreateEventDraftAiToolDefinition.AllowedPayloadFields);
    }

    [Test]
    public async Task JsonSchema_RequiresTitleAndDisallowsAdditionalProperties()
    {
        using var document = JsonDocument.Parse(CreateEventDraftAiToolDefinition.JsonSchema);
        var root = document.RootElement;
        var requiredFields = root.GetProperty("required")
            .EnumerateArray()
            .Select(field => field.GetString())
            .ToArray();

        await Assert.That(root.GetProperty("additionalProperties").GetBoolean()).IsFalse();
        await Assert.That(requiredFields).Contains("title");
    }

    [Test]
    public async Task Mapper_AcceptsEveryAllowedPayloadField()
    {
        var mapper = new CreateEventDraftAiActionMapper();

        foreach (var fieldName in CreateEventDraftAiToolDefinition.AllowedPayloadFields)
        {
            var (payloadJson, context) = CreatePayloadForAllowedField(fieldName);
            var result = mapper.Map(payloadJson, context);

            await Assert.That(result.Succeeded).IsTrue();
        }
    }

    [Test]
    public async Task Registry_RejectsEveryForbiddenPayloadFieldBeforeUnsupportedFieldHandling()
    {
        var registry = AiToolContractRegistry.CreateDefault();

        foreach (var fieldName in CreateEventDraftAiToolDefinition.ForbiddenPayloadFields)
        {
            var result = registry.ValidatePayload(
                AiProposedActionKind.CreateEventDraft,
                $"{{\"title\":\"Draft\",\"{fieldName}\":\"not allowed\"}}");

            await Assert.That(result.Succeeded).IsFalse();
            await Assert.That(result.FailureCode).IsEqualTo("forbidden_tool_argument");
        }
    }

    private static HashSet<string> GetSchemaPropertyNames()
    {
        using var document = JsonDocument.Parse(CreateEventDraftAiToolDefinition.JsonSchema);
        return document.RootElement
            .GetProperty("properties")
            .EnumerateObject()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static (string PayloadJson, CreateEventDraftAiActionMappingContext Context) CreatePayloadForAllowedField(string fieldName)
    {
        var organizationId = Guid.CreateVersion7();
        var groupId = Guid.CreateVersion7();
        var categoryId = Guid.CreateVersion7();
        var tagId = Guid.CreateVersion7();

        return fieldName switch
        {
            "title" => ("{\"title\":\"Draft\"}", CreateEventDraftAiActionMappingContext.Empty),
            "subtitle" => ("{\"title\":\"Draft\",\"subtitle\":\"Subtitle\"}", CreateEventDraftAiActionMappingContext.Empty),
            "description" => ("{\"title\":\"Draft\",\"description\":\"Short description\"}", CreateEventDraftAiActionMappingContext.Empty),
            "content" => ("{\"title\":\"Draft\",\"content\":\"Longer content\"}", CreateEventDraftAiActionMappingContext.Empty),
            "slug" => ("{\"title\":\"Draft\",\"slug\":\"draft-event\"}", CreateEventDraftAiActionMappingContext.Empty),
            "eventTypeId" => ("{\"title\":\"Draft\",\"eventTypeId\":1}", CreateEventDraftAiActionMappingContext.Empty),
            "audienceGenderId" => ("{\"title\":\"Draft\",\"audienceGenderId\":1}", CreateEventDraftAiActionMappingContext.Empty),
            "audienceAgeId" => ("{\"title\":\"Draft\",\"audienceAgeId\":1}", CreateEventDraftAiActionMappingContext.Empty),
            "organizationId" => ($"{{\"title\":\"Draft\",\"organizationId\":\"{organizationId}\"}}", new CreateEventDraftAiActionMappingContext(new HashSet<Guid> { organizationId }, new HashSet<Guid>())),
            "groupId" => ($"{{\"title\":\"Draft\",\"groupId\":\"{groupId}\"}}", new CreateEventDraftAiActionMappingContext(new HashSet<Guid>(), new HashSet<Guid> { groupId })),
            "price" => ("{\"title\":\"Draft\",\"price\":12.5}", CreateEventDraftAiActionMappingContext.Empty),
            "currencyCode" => ("{\"title\":\"Draft\",\"currencyCode\":\"EUR\"}", CreateEventDraftAiActionMappingContext.Empty),
            "isRegistrationRequired" => ("{\"title\":\"Draft\",\"isRegistrationRequired\":true}", CreateEventDraftAiActionMappingContext.Empty),
            "externalRegistrationUrl" => ("{\"title\":\"Draft\",\"externalRegistrationUrl\":\"https://example.test/register\"}", CreateEventDraftAiActionMappingContext.Empty),
            "visibilityTypeId" => ("{\"title\":\"Draft\",\"visibilityTypeId\":1}", CreateEventDraftAiActionMappingContext.Empty),
            "eventFormatId" => ("{\"title\":\"Draft\",\"eventFormatId\":1}", CreateEventDraftAiActionMappingContext.Empty),
            "madhabId" => ("{\"title\":\"Draft\",\"madhabId\":1}", CreateEventDraftAiActionMappingContext.Empty),
            "timezone" => ("{\"title\":\"Draft\",\"timezone\":\"Europe/Brussels\"}", CreateEventDraftAiActionMappingContext.Empty),
            "eventTimeZoneId" => ("{\"title\":\"Draft\",\"eventTimeZoneId\":\"Europe/Brussels\"}", CreateEventDraftAiActionMappingContext.Empty),
            "eventUrl" => ("{\"title\":\"Draft\",\"eventUrl\":\"https://example.test/event\"}", CreateEventDraftAiActionMappingContext.Empty),
            "categoryIds" => ($"{{\"title\":\"Draft\",\"categoryIds\":[\"{categoryId}\"]}}", CreateEventDraftAiActionMappingContext.Empty),
            "tagIds" => ($"{{\"title\":\"Draft\",\"tagIds\":[\"{tagId}\"]}}", CreateEventDraftAiActionMappingContext.Empty),
            _ => throw new InvalidOperationException($"No valid mapper payload sample exists for allowed AI field '{fieldName}'.")
        };
    }
}
