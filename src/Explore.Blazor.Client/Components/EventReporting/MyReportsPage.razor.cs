// ABOUTME: Code-behind for the authenticated reporter-owned event report status page.
// ABOUTME: Loads paged HAL resources through IEventReportingService and formats safe status metadata.

using System.Globalization;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Accessibility;
using Explore.Blazor.Client.Contracts.Services.EventReporting;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Components.EventReporting;

public partial class MyReportsPage : ComponentBase
{
    private const int PageSize = 10;

    [Inject] private IEventReportingService EventReportingService { get; set; } = default!;
    [Inject] private IAccessibilityAnnouncerService AnnouncerService { get; set; } = default!;

    private IReadOnlyList<HalResourceOfMyEventReportDto> _reports = [];
    private bool _isLoading = true;
    private bool _hasPrevious;
    private bool _hasNext;
    private int _pageNumber = 1;
    private int _totalPages;
    private string? _errorMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadPageAsync(1);
    }

    private Task RefreshAsync() => LoadPageAsync(_pageNumber);

    private Task PreviousPageAsync()
        => _hasPrevious ? LoadPageAsync(_pageNumber - 1) : Task.CompletedTask;

    private Task NextPageAsync()
        => _hasNext ? LoadPageAsync(_pageNumber + 1) : Task.CompletedTask;

    private async Task LoadPageAsync(int pageNumber)
    {
        _isLoading = true;
        _errorMessage = null;

        try
        {
            var result = await EventReportingService.GetMyReportsAsync(pageNumber, PageSize);
            _reports = result.Reports;
            _pageNumber = result.PageNumber;
            _totalPages = result.TotalPages;
            _hasPrevious = result.HasPrevious;
            _hasNext = result.HasNext;

            await AnnouncerService.AnnouncePoliteAsync(
                _reports.Count == 0 ? "No event reports found." : $"Loaded {_reports.Count} event reports.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            _errorMessage = "Reports could not be loaded.";
            await AnnouncerService.AnnounceAssertiveAsync(_errorMessage);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private static string GetEventHref(HalResourceOfMyEventReportDto report)
        => report.EventId is { } eventId && eventId != Guid.Empty
            ? $"/events/{eventId}"
            : "/events";

    private static string FormatEventId(Guid? eventId)
        => eventId is { } value && value != Guid.Empty
            ? $"Event {value.ToString("N")[..8]}"
            : "Event";

    private static string FormatDate(DateTimeOffset? timestamp)
        => timestamp?.ToLocalTime().ToString("MMM d, yyyy h:mm tt", CultureInfo.CurrentCulture) ?? "-";

    private static Color GetStatusColor(string? statusCode)
    {
        return statusCode switch
        {
            "submitted" or "triaged" => Color.Info,
            "under_review" or "escalated" => Color.Warning,
            "actioned" => Color.Success,
            "dismissed" or "duplicate" => Color.Default,
            "closed" => Color.Secondary,
            _ => Color.Default
        };
    }
}
