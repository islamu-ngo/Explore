// ABOUTME: Determines whether the current user should see the "Create Event" button in the nav menu.
// ABOUTME: Combines tenant policy (AllowUserSubmittedEvents) with org membership role authority.

using Explore.Blazor.Client.Helpers;

namespace Explore.Blazor.Client.Services;

/// <summary>
/// Resolves whether the current authenticated user is eligible to create events
/// based on the active tenant policy and the user's organization memberships.
/// </summary>
public interface IEventCreationEligibilityService
{
    /// <summary>
    /// Returns the event creation eligibility result for the current user.
    /// Contains the eligibility flag and, when in org-only mode,
    /// the first eligible organization ID for the create-event route.
    /// </summary>
    Task<EventCreationEligibility> GetEligibilityAsync();
}

/// <summary>
/// Immutable result of the event creation eligibility check.
/// </summary>
public sealed record EventCreationEligibility
{
    public static readonly EventCreationEligibility NotEligible = new();

    /// <summary>
    /// Whether the user may create events.
    /// </summary>
    public bool CanCreate { get; init; }

    /// <summary>
    /// When true, the tenant allows any authenticated user to submit events.
    /// The create-event route does not require an organization context.
    /// </summary>
    public bool IsUserSubmissionMode { get; init; }

    /// <summary>
    /// In org-only mode, the first organization ID where the user can create events.
    /// Null when in user-submission mode or when user has no eligible org.
    /// </summary>
    public Guid? EligibleOrganizationId { get; init; }

    /// <summary>
    /// Resolves the appropriate navigation route for the "Create Event" action.
    /// </summary>
    public string CreateEventRoute => IsUserSubmissionMode
        ? "/create-event"
        : $"/organization/{EligibleOrganizationId}/create-event";
}

public class EventCreationEligibilityService : IEventCreationEligibilityService
{
    private readonly IPublicExperienceService _publicExperienceService;
    private readonly IOrganizationService _organizationService;
    private readonly ILogger<EventCreationEligibilityService> _logger;

    public EventCreationEligibilityService(
        IPublicExperienceService publicExperienceService,
        IOrganizationService organizationService,
        ILogger<EventCreationEligibilityService> logger)
    {
        _publicExperienceService = publicExperienceService;
        _organizationService = organizationService;
        _logger = logger;
    }

    public async Task<EventCreationEligibility> GetEligibilityAsync()
    {
        try
        {
            var settings = await _publicExperienceService.GetCachedSettingsAsync();

            if (settings?.AllowUserSubmittedEvents == true)
            {
                return new EventCreationEligibility
                {
                    CanCreate = true,
                    IsUserSubmissionMode = true
                };
            }

            // Org-only mode: check if the user has event:create permission in any org.
            // The org API returns CurrentUserRole which maps to RoleEnum IDs.
            var orgs = await _organizationService.GetMyOrganizationsAsync();

            var eligibleOrg = orgs.FirstOrDefault(o => RoleHelper.CanManage(o.CurrentUserRole));

            if (eligibleOrg is null)
            {
                return EventCreationEligibility.NotEligible;
            }

            return new EventCreationEligibility
            {
                CanCreate = true,
                IsUserSubmissionMode = false,
                EligibleOrganizationId = eligibleOrg.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve event creation eligibility.");
            return EventCreationEligibility.NotEligible;
        }
    }
}
