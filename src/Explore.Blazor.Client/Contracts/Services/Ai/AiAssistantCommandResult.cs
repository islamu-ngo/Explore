// ABOUTME: Blazor AI assistant command result model returned by the client service wrapper.
// ABOUTME: Keeps generated API command response mapping out of the pure service interface file.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Ai;

public sealed record AiAssistantCommandResult(
    bool Success,
    Guid? Id,
    string? Message,
    string? FailureCode,
    IReadOnlyList<string> Errors)
{
    public static AiAssistantCommandResult FromResponse(BaseCommandResponseOfGuid? response)
    {
        if (response is null)
        {
            return Failure("empty_response", "The AI assistant API returned an empty response.");
        }

        return new AiAssistantCommandResult(
            response.Success == true,
            response.Id,
            response.Message,
            response.FailureCode,
            response.Errors?.ToList() ?? []);
    }

    public static AiAssistantCommandResult Failure(string failureCode, string message) =>
        new(false, null, message, failureCode, [message]);
}
