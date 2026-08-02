// ABOUTME: Adapts attendee-safe native registration HAL resources into renderer workflow state.
// ABOUTME: Keeps order and attempt capabilities in memory and sends explicit idempotency keys for every write.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Components.Registration.FormRenderer;
using Explore.Blazor.Client.Contracts.Services;

namespace Explore.Blazor.Client.Services;

public sealed class NativeRegistrationFormService(
    IEventApiClient apiClient,
    ILogger<NativeRegistrationFormService> logger) : INativeRegistrationFormService
{
    public async Task<NativeRegistrationRequirementCollectionView?> GetRequirementsAsync(
        Guid eventId, Guid orderId, GuestRegistrationOrderCapability? guestCapability,
        CancellationToken cancellationToken = default)
    {
        try
        {
            HalResourceOfNativeRegistrationRequirementProgressCollectionDto source = guestCapability is { } guest
                ? await apiClient.GetGuestNativeRegistrationRequirementProgressAsync(
                    eventId, orderId, guest.Value, cancellationToken: cancellationToken)
                : await apiClient.GetAuthenticatedNativeRegistrationRequirementProgressAsync(
                    eventId, orderId, cancellationToken: cancellationToken);
            return NativeRegistrationRequirementCollectionView.From(source);
        }
        catch (ApiException exception)
        {
            logger.LogWarning("Native registration requirements were unavailable. Status: {StatusCode}.", exception.StatusCode);
            return null;
        }
    }

    public async Task<NativeRegistrationAttemptView?> LaunchAsync(
        Guid eventId, Guid orderId, NativeRegistrationLaunchDescriptorView descriptor,
        GuestRegistrationOrderCapability? guestCapability, string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var request = new LaunchNativeRegistrationAttemptRequest
        {
            RequirementId = descriptor.RequirementId,
            ChannelId = descriptor.ChannelId,
            FormId = descriptor.FormId,
            FormVersionId = descriptor.FormVersionId
        };
        try
        {
            HalResourceOfNativeRegistrationAttemptDto source = guestCapability is { } guest
                ? await apiClient.LaunchGuestNativeRegistrationAttemptAsync(
                    eventId, orderId, request, guest.Value, idempotencyKey, cancellationToken: cancellationToken)
                : await apiClient.LaunchAuthenticatedNativeRegistrationAttemptAsync(
                    eventId, orderId, request, idempotencyKey, cancellationToken: cancellationToken);
            return NativeRegistrationAttemptView.From(source);
        }
        catch (ApiException exception)
        {
            logger.LogWarning("Native registration attempt could not be launched. Status: {StatusCode}.", exception.StatusCode);
            return null;
        }
    }

    public async Task<NativeRegistrationActionResult> SubmitAsync(
        Guid eventId, Guid orderId, NativeRegistrationAttemptView attempt,
        RegistrationSubjectView subject, RegistrationFormSubmission submission,
        GuestRegistrationOrderCapability? guestCapability, string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var request = new SubmitNativeRegistrationAttemptRequest
        {
            RequirementId = attempt.RequirementId,
            Answers = submission.Answers.Select(answer => new NativeRegistrationSubmissionAnswerRequest
            {
                FieldId = answer.Key,
                SubjectType = subject.SubjectType,
                SubjectId = subject.SubjectId,
                TicketAssignmentOrderLineId = subject.TicketAssignmentOrderLineId,
                Value = answer.Value!
            }).ToArray()
        };
        try
        {
            NativeRegistrationSubmissionDto response = guestCapability is { } guest
                ? await apiClient.SubmitGuestNativeRegistrationAttemptAsync(
                    eventId, orderId, attempt.AttemptId, request, guest.Value,
                    attempt.AttemptCapabilityToken, idempotencyKey, cancellationToken: cancellationToken)
                : await apiClient.SubmitAuthenticatedNativeRegistrationAttemptAsync(
                    eventId, orderId, attempt.AttemptId, request,
                    attempt.AttemptCapabilityToken, idempotencyKey, cancellationToken: cancellationToken);
            return response.Accepted ? NativeRegistrationActionResult.Accepted : NativeRegistrationActionResult.Failed;
        }
        catch (ApiException<ValidationProblemDetails> exception)
        {
            return NativeRegistrationActionResult.Invalid(MapIssues(attempt, exception.Result?.Errors));
        }
        catch (ApiException exception)
        {
            logger.LogWarning("Native registration answers could not be submitted. Status: {StatusCode}.", exception.StatusCode);
            return NativeRegistrationActionResult.Failed;
        }
    }

    public async Task<bool> SkipAsync(
        Guid eventId, Guid orderId, NativeRegistrationAttemptView attempt,
        GuestRegistrationOrderCapability? guestCapability, string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var request = new SkipNativeRegistrationRequirementRequest { RequirementId = attempt.RequirementId };
        try
        {
            _ = guestCapability is { } guest
                ? await apiClient.SkipGuestNativeRegistrationRequirementAsync(
                    eventId, orderId, attempt.AttemptId, request, guest.Value,
                    attempt.AttemptCapabilityToken, idempotencyKey, cancellationToken: cancellationToken)
                : await apiClient.SkipAuthenticatedNativeRegistrationRequirementAsync(
                    eventId, orderId, attempt.AttemptId, request,
                    attempt.AttemptCapabilityToken, idempotencyKey, cancellationToken: cancellationToken);
            return true;
        }
        catch (ApiException exception)
        {
            logger.LogWarning("Native registration requirement could not be skipped. Status: {StatusCode}.", exception.StatusCode);
            return false;
        }
    }

    private static IReadOnlyDictionary<Guid, IReadOnlyList<string>> MapIssues(
        NativeRegistrationAttemptView attempt,
        IDictionary<string, ICollection<string>>? errors)
    {
        if (errors is null) return new Dictionary<Guid, IReadOnlyList<string>>();
        Dictionary<string, Guid> fields = attempt.Sections.SelectMany(section => section.Fields)
            .GroupBy(field => field.Key, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single().Id, StringComparer.OrdinalIgnoreCase);
        return errors
            .Where(error => fields.ContainsKey(error.Key))
            .ToDictionary(error => fields[error.Key], error => (IReadOnlyList<string>)error.Value
                .Select(SafeIssueMessage).Distinct(StringComparer.Ordinal).ToArray());
    }

    private static string SafeIssueMessage(string code) => code switch
    {
        "REQUIRED_FIELD_MISSING" => "Enter an answer.",
        "LENGTH_OUT_OF_RANGE" => "Use the allowed number of characters.",
        "INVALID_OPTION" => "Choose one of the available options.",
        _ => "Review the format of this answer."
    };
}
