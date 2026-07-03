// ABOUTME: Reporter-facing Blazor service that wraps generated event-report API calls.
// ABOUTME: Converts HAL collections and ProblemDetails failures into stable UI result models.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.EventReporting;
using Explore.Blazor.Client.Helpers;

namespace Explore.Blazor.Client.Services;

public sealed class EventReportingService(
    IEventApiClient apiClient,
    ILogger<EventReportingService> logger)
    : IEventReportingService
{
    public async Task<HalResourceOfEventReportOptionsDto?> GetOptionsAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await apiClient.GetEventReportOptionsAsync(eventId, cancellationToken: cancellationToken);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            logger.LogInformation("Event-report options were not found for event {EventId}", eventId);
            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not load event-report options for event {EventId}", eventId);
            return null;
        }
    }

    public async Task<EventReportSubmissionResult> SubmitAsync(
        SubmitEventReportDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await apiClient.SubmitEventReportAsync(request, cancellationToken: cancellationToken);
            return response.Success == true
                ? EventReportSubmissionResult.Successful(response.Id, response.Message)
                : EventReportSubmissionResult.Failed(response.Message, response.Errors, response.FailureCode);
        }
        catch (ApiException<ProblemDetails> ex)
        {
            logger.LogWarning(
                ex,
                "Event-report submission failed with ProblemDetails status {StatusCode} for event {EventId}",
                ex.StatusCode,
                request.EventId);

            return EventReportSubmissionResult.Failed(
                ex.Result.Detail ?? ex.Result.Title,
                failureCode: TryGetProblemCode(ex.Result));
        }
        catch (ApiException ex)
        {
            logger.LogWarning(
                ex,
                "Event-report submission failed with API status {StatusCode} for event {EventId}",
                ex.StatusCode,
                request.EventId);

            return EventReportSubmissionResult.Failed("Event report could not be submitted.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected event-report submission failure for event {EventId}", request.EventId);
            return EventReportSubmissionResult.Failed("Event report could not be submitted.");
        }
    }

    public async Task<EventReportPageResult> GetMyReportsAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await apiClient.GetMyEventReportsAsync(
                pageNumber,
                pageSize,
                cancellationToken: cancellationToken);

            return new EventReportPageResult(
                response.GetItems().ToArray(),
                response.PageNumber ?? pageNumber,
                response.PageSize ?? pageSize,
                response.TotalCount ?? 0,
                response.TotalPages ?? 0,
                response.HasPrevious == true,
                response.HasNext == true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not load reporter-owned event reports page {PageNumber}", pageNumber);
            return EventReportPageResult.Empty(pageNumber, pageSize);
        }
    }

    public async Task<HalResourceOfMyEventReportDto?> GetMyReportAsync(
        Guid reportId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await apiClient.GetMyEventReportAsync(reportId, cancellationToken: cancellationToken);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not load reporter-owned event report {ReportId}", reportId);
            return null;
        }
    }

    private static string? TryGetProblemCode(ProblemDetails problem)
    {
        if (problem.AdditionalProperties.TryGetValue("code", out var value))
        {
            return value?.ToString();
        }

        return null;
    }
}
