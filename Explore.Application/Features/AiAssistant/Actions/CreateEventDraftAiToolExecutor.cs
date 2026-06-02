// ABOUTME: Executes confirmed CreateEventDraft AI actions through the canonical CreateEventCommand.
// ABOUTME: Keeps AI tool execution behind MediatR so event creation validation and authorization stay centralized.

using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.AiAssistant.Actions;

public sealed class CreateEventDraftAiToolExecutor(IMediator mediator)
{
    private readonly CreateEventDraftAiActionMapper _mapper = new();

    public async Task<CreateEventDraftAiToolExecutionResult> ExecuteAsync(
        string payloadJson,
        CreateEventDraftAiActionMappingContext? mappingContext,
        CancellationToken cancellationToken)
    {
        var mappingResult = _mapper.Map(payloadJson, mappingContext);
        if (!mappingResult.Succeeded)
        {
            return CreateEventDraftAiToolExecutionResult.Failure(
                mappingResult.FailureCode ?? "invalid_tool_arguments",
                mappingResult.FailureMessage ?? "AI event draft payload could not be mapped.");
        }

        BaseCommandResponse<Guid> createResult = await mediator.Send(new CreateEventCommand
        {
            Request = mappingResult.Draft!.ToCreateEventRequest()
        }, cancellationToken);

        if (!createResult.Success || createResult.Id == Guid.Empty)
        {
            return CreateEventDraftAiToolExecutionResult.Failure(
                createResult.FailureCode ?? "event_creation_failed",
                createResult.Message ?? "AI event draft confirmation could not create an event.");
        }

        return CreateEventDraftAiToolExecutionResult.Success(createResult.Id);
    }
}

public sealed record CreateEventDraftAiToolExecutionResult(
    bool Succeeded,
    Guid? ResultResourceId,
    string? FailureCode,
    string? FailureMessage)
{
    public static CreateEventDraftAiToolExecutionResult Success(Guid resultResourceId)
        => new(true, resultResourceId, null, null);

    public static CreateEventDraftAiToolExecutionResult Failure(string failureCode, string failureMessage)
        => new(false, null, failureCode, failureMessage);
}
