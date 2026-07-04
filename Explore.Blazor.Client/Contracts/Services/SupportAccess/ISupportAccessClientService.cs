// ABOUTME: Client-side contract for BFF-mediated support-access session status and commands.
// ABOUTME: Keeps Razor components dependent on server-confirmed state instead of browser claims.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.SupportAccess;

public interface ISupportAccessClientService
{
    event Action? Changed;

    SupportAccessSessionDto? CurrentSession { get; }

    bool IsActive { get; }

    bool IsLoading { get; }

    bool IsStopping { get; }

    string? LastError { get; }

    Task RefreshAsync(CancellationToken cancellationToken = default);

    Task<SupportAccessCommandResult> StartAsync(
        StartSupportAccessSessionRequestDto request,
        CancellationToken cancellationToken = default);

    Task<SupportAccessCommandResult> StopCurrentAsync(
        string? endReasonText = null,
        CancellationToken cancellationToken = default);

    Task<SupportAccessSessionCollection> GetSessionsAsync(
        Guid targetTenantId,
        int limit = 100,
        CancellationToken cancellationToken = default);

    Task<SupportAccessAuditEventCollection> GetAuditEventsAsync(
        Guid targetTenantId,
        Guid sessionId,
        int limit = 100,
        CancellationToken cancellationToken = default);

    Task<SupportAccessCommandResult> ForceStopAsync(
        Guid sessionId,
        string? endReasonText = null,
        CancellationToken cancellationToken = default);
}
