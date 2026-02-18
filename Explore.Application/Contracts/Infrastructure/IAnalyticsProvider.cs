// ABOUTME: Provider-agnostic contract for analytics tracking across the application.
// ABOUTME: Implemented by PostHogAnalyticsProvider, PlausibleAnalyticsProvider, RybbitAnalyticsProvider, RudderStackAnalyticsProvider, and NullAnalyticsProvider.

namespace Explore.Application.Contracts.Infrastructure;

/// <summary>
/// Analytics provider that abstracts event tracking, user identification, and group analytics.
/// <para>
/// Implementations include PostHogAnalyticsProvider, PlausibleAnalyticsProvider, RybbitAnalyticsProvider, RudderStackAnalyticsProvider, and NullAnalyticsProvider.
/// Runtime switching is handled by RuntimeAnalyticsProvider wrapper via SystemSetting.
/// </para>
/// <para>
/// All implementations must be fire-and-forget safe — analytics failures must NEVER break business logic.
/// </para>
/// </summary>
public interface IAnalyticsProvider
{
    /// <summary>
    /// Identifies a user with optional traits (e.g., email, name, plan).
    /// Creates or updates the user profile in the analytics provider.
    /// </summary>
    /// <param name="distinctId">Unique user identifier (typically Keycloak sub claim).</param>
    /// <param name="traits">Optional user properties to set (e.g., email, name, tenantId).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task IdentifyAsync(
        string distinctId,
        IDictionary<string, object>? traits = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks a named event with optional properties.
    /// </summary>
    /// <param name="distinctId">Unique user identifier.</param>
    /// <param name="eventName">Event name (e.g., "Event Created", "User Signed Up").</param>
    /// <param name="properties">Optional event properties (e.g., eventId, category).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task TrackAsync(
        string distinctId,
        string eventName,
        IDictionary<string, object>? properties = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks a page view event.
    /// </summary>
    /// <param name="distinctId">Unique user identifier.</param>
    /// <param name="pagePath">The page path being viewed (e.g., "/events/123").</param>
    /// <param name="properties">Optional additional properties.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PageViewAsync(
        string distinctId,
        string pagePath,
        IDictionary<string, object>? properties = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Associates a user with a group (e.g., tenant, organization).
    /// </summary>
    /// <param name="groupType">The group type (e.g., "tenant", "organization").</param>
    /// <param name="groupKey">The group identifier.</param>
    /// <param name="properties">Optional group properties (e.g., name, plan).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task GroupIdentifyAsync(
        string groupType,
        string groupKey,
        IDictionary<string, object>? properties = null,
        CancellationToken cancellationToken = default);
}
