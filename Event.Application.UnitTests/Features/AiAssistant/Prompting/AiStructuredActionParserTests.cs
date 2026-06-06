// ABOUTME: Unit tests for validating untrusted AI provider proposed actions.
// ABOUTME: Ensures only registry-approved JSON-object action payloads can become persisted proposals.

using Explore.Application.Contracts.Infrastructure.Ai;
using Explore.Application.Features.AiAssistant.Prompting;
using Explore.Application.Features.AiAssistant.Tools;
using Explore.Domain.Ai;

namespace Event.Application.UnitTests.Features.AiAssistant.Prompting;

public sealed class AiStructuredActionParserTests
{
    [Test]
    public async Task Parse_WhenPayloadIsJsonObject_ReturnsParsedAction()
    {
        var result = new AiStructuredActionParser().Parse(
            [new AiProposedActionCandidate(AiProposedActionKind.CreateEventDraft, "{\"title\":\"Draft\"}", "Create draft")]);

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.Actions.Count).IsEqualTo(1);
        await Assert.That(result.Actions[0].Kind).IsEqualTo(AiProposedActionKind.CreateEventDraft);
        await Assert.That(result.Actions[0].Summary).IsEqualTo("Create draft");
    }

    [Test]
    public async Task Parse_WhenPayloadIsInvalidJson_ReturnsInvalidToolArgumentsFailure()
    {
        var result = new AiStructuredActionParser().Parse(
            [new AiProposedActionCandidate(AiProposedActionKind.CreateEventDraft, "not-json")]);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("invalid_tool_arguments");
    }

    [Test]
    public async Task Parse_WhenPayloadIsJsonArray_ReturnsInvalidToolArgumentsFailure()
    {
        var result = new AiStructuredActionParser().Parse(
            [new AiProposedActionCandidate(AiProposedActionKind.CreateEventDraft, "[]")]);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("invalid_tool_arguments");
    }

    [Test]
    public async Task Parse_WhenPayloadFailsSchemaValidation_ReturnsCorrectionMessage()
    {
        var result = new AiStructuredActionParser().Parse(
            [new AiProposedActionCandidate(AiProposedActionKind.CreateEventDraft, "{\"description\":\"Missing title\"}")]);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("missing_tool_argument");
        await Assert.That(result.FailureMessage).DoesNotContain("title");
        await Assert.That(result.CorrectionMessage).Contains("matches the registered schema exactly");
        await Assert.That(result.CorrectionMessage).DoesNotContain("Missing title");
    }

    [Test]
    public async Task Parse_WhenPayloadContainsForbiddenField_ReturnsForbiddenToolArgumentFailure()
    {
        var result = new AiStructuredActionParser().Parse(
            [new AiProposedActionCandidate(AiProposedActionKind.CreateEventDraft, "{\"title\":\"Draft\",\"eventStatusId\":2}")]);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("forbidden_tool_argument");
        await Assert.That(result.FailureMessage).DoesNotContain("eventStatusId");
    }

    [Test]
    public async Task Parse_WhenActionKindIsUnknown_ReturnsUnknownActionFailure()
    {
        var result = new AiStructuredActionParser().Parse(
            [new AiProposedActionCandidate((AiProposedActionKind)999, "{\"title\":\"Draft\"}")]);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("unknown_action_kind");
    }

    [Test]
    public async Task Parse_WhenRegistryDoesNotContainKind_ReturnsUnknownActionFailure()
    {
        var result = new AiStructuredActionParser(new AiToolContractRegistry([])).Parse(
            [new AiProposedActionCandidate(AiProposedActionKind.CreateEventDraft, "{\"title\":\"Draft\"}")]);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("unknown_action_kind");
    }
}
