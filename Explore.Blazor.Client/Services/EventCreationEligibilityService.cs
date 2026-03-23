// ABOUTME: Determines whether the current user should see the "Create Event" button in the nav menu.
// ABOUTME: Combines tenant policy (user/org/group submission) with org/group membership role authority.

using Explore.Blazor.Client.Helpers;

namespace Explore.Blazor.Client.Services;

/// <summary>
/// Resolves whether the current authenticated user is eligible to create events
/// based on the active tenant policy and the user's organization/group memberships.
/// </summary>
public interface IEventCreationEligibilityService
{
    /// <summary>
    /// Returns the event creation eligibility result for the current user.
    /// Contains the eligibility flag and, when in org/group-only mode,
    /// the first eligible entity ID for the create-event route.
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
    /// In group-only mode, the first group ID where the user can create events.
    /// Null when in user/org-submission mode or when user has no eligible group.
    /// </summary>
    public Guid? EligibleGroupId { get; init; }

    /// <summary>
    /// Resolves the appropriate navigation route for the "Create Event" action.
    /// </summary>
    public string CreateEventRoute => IsUserSubmissionMode
        ? "/events/create"
        : EligibleOrganizationId.HasValue
            ? $"/organizations/{EligibleOrganizationId}/events/create"
            : EligibleGroupId.HasValue
                ? $"/groups/{EligibleGroupId}/events/create"
                : "/events/create";
}

public class EventCreationEligibilityService : IEventCreationEligibilityService
{
    private readonly IPublicExperienceService _publicExperienceService;
    private readonly IOrganizationService _organizationService;
    private readonly IGroupService _groupService;
    private readonly ILogger<EventCreationEligibilityService> _logger;

    public EventCreationEligibilityService(
        IPublicExperienceService publicExperienceService,
        IOrganizationService organizationService,
        IGroupService groupService,
        ILogger<EventCreationEligibilityService> logger)
    {
        _publicExperienceService = publicExperienceService;
        _organizationService = organizationService;
        _groupService = groupService;
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

            // Check org membership when org submission is enabled
            if (settings?.AllowOrganizationSubmittedEvents == true)
            {
                var orgs = await _organizationService.GetMyOrganizationsAsync();
                var eligibleOrg = orgs.FirstOrDefault(o => RoleHelper.CanManage(o.CurrentUserRole));

                if (eligibleOrg is not null)
                {
                    return new EventCreationEligibility
                    {
                        CanCreate = true,
                        IsUserSubmissionMode = false,
                        EligibleOrganizationId = eligibleOrg.Id
                    };
                }
            }

            // Check group membership when group submission is enabled
            if (settings?.AllowGroupSubmittedEvents == true)
            {
                var groups = await _groupService.GetMyGroupsAsync();
                var eligibleGroup = groups.FirstOrDefault(g => RoleHelper.CanManage(g.CurrentUserRole));

                if (eligibleGroup is not null)
                {
                    return new EventCreationEligibility
                    {
                        CanCreate = true,
                        IsUserSubmissionMode = false,
                        EligibleGroupId = eligibleGroup.Id
                    };
                }
            }

            return EventCreationEligibility.NotEligible;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve event creation eligibility.");
            return EventCreationEligibility.NotEligible;
        }
    }
}
