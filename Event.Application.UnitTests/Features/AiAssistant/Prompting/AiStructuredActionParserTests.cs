// ABOUTME: Unit tests for validating untrusted AI provider proposed actions.
// ABOUTME: Ensures only allow-listed JSON-object action payloads can become persisted proposals.

using Explore.Application.Contracts.Infrastructure.Ai;
using Explore.Application.Features.AiAssistant.Prompting;
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
    public async Task Parse_WhenActionKindIsUnknown_ReturnsUnknownActionFailure()
    {
        var result = new AiStructuredActionParser().Parse(
            [new AiProposedActionCandidate((AiProposedActionKind)999, "{\"title\":\"Draft\"}")]);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("unknown_action_kind");
    }
}
