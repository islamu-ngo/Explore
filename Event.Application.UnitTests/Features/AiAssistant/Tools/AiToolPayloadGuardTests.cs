// ABOUTME: Unit tests for the shared AI tool payload guard.
// ABOUTME: Verifies malformed, non-object, unknown, and forbidden payload fields fail closed safely.

using Explore.Application.Features.AiAssistant.Tools;

namespace Event.Application.UnitTests.Features.AiAssistant.Tools;

public sealed class AiToolPayloadGuardTests
{
    private static readonly HashSet<string> AllowedFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "title",
        "description",
        "eventTypeId",
        "organizationId",
        "price",
        "currencyCode"
    };

    private const string SchemaJson = """
        {
          "type": "object",
          "additionalProperties": false,
          "required": ["title"],
          "properties": {
            "title": { "type": "string" },
            "description": { "type": "string" },
            "eventTypeId": { "type": "integer" },
            "organizationId": { "type": "string", "format": "uuid" },
            "price": { "type": "number", "minimum": 0 },
            "currencyCode": { "type": "string", "maxLength": 3 }
          }
        }
        """;

    private static readonly HashSet<string> ForbiddenFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "tenantId",
        "eventStatusId"
    };

    [Test]
    public async Task ValidateJsonObject_WhenPayloadIsAllowedObject_ReturnsSuccess()
    {
        var result = AiToolPayloadGuard.ValidateJsonObject(
            "{\"title\":\"Draft\",\"description\":\"Details\"}",
            AllowedFields,
            ForbiddenFields);

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.FailureCode).IsNull();
    }

    [Test]
    public async Task ValidateJsonObject_WhenPayloadIsInvalidJson_ReturnsInvalidToolArgumentsFailure()
    {
        var result = AiToolPayloadGuard.ValidateJsonObject("not-json", AllowedFields, ForbiddenFields);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("invalid_tool_arguments");
        await Assert.That(result.FailureMessage).DoesNotContain("not-json");
        await Assert.That(result.CorrectionMessage).Contains("Regenerate the tool call arguments");
    }

    [Test]
    public async Task ValidateJsonObject_WhenPayloadIsArray_ReturnsInvalidToolArgumentsFailure()
    {
        var result = AiToolPayloadGuard.ValidateJsonObject("[]", AllowedFields, ForbiddenFields);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("invalid_tool_arguments");
    }

    [Test]
    public async Task ValidateJsonObject_WhenPayloadContainsUnknownField_ReturnsUnsupportedToolArgumentFailure()
    {
        var result = AiToolPayloadGuard.ValidateJsonObject(
            "{\"title\":\"Draft\",\"unexpected\":true}",
            AllowedFields,
            ForbiddenFields);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("unsupported_tool_argument");
        await Assert.That(result.FailureMessage).DoesNotContain("unexpected");
    }

    [Test]
    public async Task ValidateJsonObject_WhenPayloadContainsForbiddenField_ReturnsForbiddenToolArgumentFailure()
    {
        var result = AiToolPayloadGuard.ValidateJsonObject(
            "{\"title\":\"Draft\",\"tenantId\":\"not allowed\"}",
            AllowedFields,
            ForbiddenFields);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("forbidden_tool_argument");
        await Assert.That(result.FailureMessage).DoesNotContain("tenantId");
    }

    [Test]
    public async Task ValidateJsonObject_WhenSchemaRequiredFieldIsMissing_ReturnsSafeCorrectionFailure()
    {
        var result = AiToolPayloadGuard.ValidateJsonObject(
            "{\"description\":\"Details\"}",
            AllowedFields,
            ForbiddenFields,
            SchemaJson);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("missing_tool_argument");
        await Assert.That(result.FailureMessage).DoesNotContain("title");
        await Assert.That(result.CorrectionMessage).Contains("matches the registered schema exactly");
    }

    [Test]
    public async Task ValidateJsonObject_WhenSchemaIntegerTypeIsWrong_ReturnsSafeTypeFailure()
    {
        var result = AiToolPayloadGuard.ValidateJsonObject(
            "{\"title\":\"Draft\",\"eventTypeId\":\"one\"}",
            AllowedFields,
            ForbiddenFields,
            SchemaJson);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("invalid_tool_argument_type");
        await Assert.That(result.FailureMessage).DoesNotContain("eventTypeId");
        await Assert.That(result.FailureMessage).DoesNotContain("one");
        await Assert.That(result.CorrectionMessage).Contains("documented JSON types and formats");
    }

    [Test]
    public async Task ValidateJsonObject_WhenSchemaUuidFormatIsInvalid_ReturnsSafeFormatFailure()
    {
        var result = AiToolPayloadGuard.ValidateJsonObject(
            "{\"title\":\"Draft\",\"organizationId\":\"not-a-guid\"}",
            AllowedFields,
            ForbiddenFields,
            SchemaJson);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("invalid_tool_argument_format");
        await Assert.That(result.FailureMessage).DoesNotContain("organizationId");
        await Assert.That(result.FailureMessage).DoesNotContain("not-a-guid");
    }

    [Test]
    public async Task ValidateJsonObject_WhenSchemaNumberIsBelowMinimum_ReturnsSafeValueFailure()
    {
        var result = AiToolPayloadGuard.ValidateJsonObject(
            "{\"title\":\"Draft\",\"price\":-1}",
            AllowedFields,
            ForbiddenFields,
            SchemaJson);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("invalid_tool_argument_value");
        await Assert.That(result.FailureMessage).DoesNotContain("price");
        await Assert.That(result.FailureMessage).DoesNotContain("-1");
    }

    [Test]
    public async Task ValidateJsonObject_WhenSchemaStringIsTooLong_ReturnsSafeValueFailure()
    {
        var result = AiToolPayloadGuard.ValidateJsonObject(
            "{\"title\":\"Draft\",\"currencyCode\":\"EURO\"}",
            AllowedFields,
            ForbiddenFields,
            SchemaJson);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("invalid_tool_argument_value");
        await Assert.That(result.FailureMessage).DoesNotContain("currencyCode");
        await Assert.That(result.FailureMessage).DoesNotContain("EURO");
    }

    [Test]
    public async Task ValidatePayload_WhenRegistryHasDefinition_UsesDefinitionFieldPolicy()
    {
        var registry = new AiToolContractRegistry(
        [
            new AiToolDefinition(
                Explore.Domain.Ai.AiProposedActionKind.CreateEventDraft,
                "CreateEventDraft",
                "Create event draft",
                "{}",
                AllowedFields,
                ForbiddenFields)
        ]);

        var result = registry.ValidatePayload(
            Explore.Domain.Ai.AiProposedActionKind.CreateEventDraft,
            "{\"title\":\"Draft\"}");

        await Assert.That(result.Succeeded).IsTrue();
    }

    [Test]
    public async Task ValidatePayload_WhenRegistryDoesNotKnowKind_ReturnsUnknownActionKindFailure()
    {
        var registry = new AiToolContractRegistry([]);

        var result = registry.ValidatePayload(
            Explore.Domain.Ai.AiProposedActionKind.CreateEventDraft,
            "{\"title\":\"Draft\"}");

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("unknown_action_kind");
    }
}
