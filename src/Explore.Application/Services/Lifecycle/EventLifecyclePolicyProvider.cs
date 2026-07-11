// ABOUTME: Default implementation composing hard invariants and tenant/instance policy overrides.
// ABOUTME: Hard invariants are non-negotiable; tenant overrides can only tighten, never loosen them.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Services;

namespace Explore.Application.Services.Lifecycle;

/// <summary>
/// Default implementation of <see cref="IEventLifecyclePolicyProvider"/>.
/// Composes hard invariants (always required for a profile) with optional
/// tenant/instance policy overrides via <see cref="ITenantPolicySettingService"/>.
/// Tenant overrides may only tighten requirements, never loosen hard invariants.
/// </summary>
public sealed class EventLifecyclePolicyProvider : IEventLifecyclePolicyProvider
{
    private readonly ITenantPolicySettingService _tenantPolicySettingService;

    public EventLifecyclePolicyProvider(ITenantPolicySettingService tenantPolicySettingService)
    {
        _tenantPolicySettingService = tenantPolicySettingService;
    }

    /// <inheritdoc />
    public async Task<EventLifecyclePolicy> GetEffectivePolicyAsync(
        Guid? tenantId,
        ValidationProfile profile,
        CancellationToken cancellationToken)
    {
        EventLifecyclePolicy basePolicy = BuildHardInvariantPolicy(profile);

        if (tenantId is null)
        {
            return basePolicy;
        }

        // Tenant composition hook: future work can merge tenant policy settings
        // (e.g., stricter hosted-instance publication requirements) on top of the
        // hard invariants. For now, we preserve the base policy and note the source.
        // This keeps the central composition point wired without premature coupling.
        _ = await _tenantPolicySettingService.ReadEffectiveTenantSettingsAsync(tenantId.Value);

        return basePolicy with { Source = "tenant-aware" };
    }

    private static EventLifecyclePolicy BuildHardInvariantPolicy(ValidationProfile profile)
    {
        return profile switch
        {
            ValidationProfile.EventDraftCreate => new EventLifecyclePolicy
            {
                Profile = profile,
                RequiredEventFields = new HashSet<Enum>
                {
                    EventFieldKey.Title,
                    EventFieldKey.Tenant,
                    EventFieldKey.Owner,
                    EventFieldKey.Status
                },
                RequiredSessionFields = new HashSet<Enum>()
            },
            ValidationProfile.EventNativeSubmit => new EventLifecyclePolicy
            {
                Profile = profile,
                RequiredEventFields = new HashSet<Enum>
                {
                    EventFieldKey.Title,
                    EventFieldKey.Tenant,
                    EventFieldKey.Owner,
                    EventFieldKey.Status,
                    EventFieldKey.Visibility,
                    EventFieldKey.Format,
                    EventFieldKey.Type,
                    EventFieldKey.AudienceGender,
                    EventFieldKey.AudienceAge
                },
                RequiredSessionFields = new HashSet<Enum>()
            },
            ValidationProfile.EventImportCreate => new EventLifecyclePolicy
            {
                Profile = profile,
                RequiredEventFields = new HashSet<Enum>
                {
                    EventFieldKey.Title,
                    EventFieldKey.Tenant,
                    EventFieldKey.Owner,
                    EventFieldKey.Status,
                    EventFieldKey.ProvenanceSource,
                    EventFieldKey.ProvenanceExternalId
                },
                RequiredSessionFields = new HashSet<Enum>()
            },
            ValidationProfile.EventArchiveCreate => new EventLifecyclePolicy
            {
                Profile = profile,
                RequiredEventFields = new HashSet<Enum>
                {
                    EventFieldKey.Title,
                    EventFieldKey.Tenant,
                    EventFieldKey.Owner,
                    EventFieldKey.Status
                },
                RequiredSessionFields = new HashSet<Enum>()
            },
            ValidationProfile.EventPublish => new EventLifecyclePolicy
            {
                Profile = profile,
                RequiredEventFields = new HashSet<Enum>
                {
                    EventFieldKey.Title,
                    EventFieldKey.Tenant,
                    EventFieldKey.Owner,
                    EventFieldKey.Status,
                    EventFieldKey.Visibility,
                    EventFieldKey.Format,
                    EventFieldKey.ScheduleSessions,
                    EventFieldKey.ScheduleFirstStart
                },
                RequiredSessionFields = new HashSet<Enum>()
            },
            ValidationProfile.SessionDraftCreate => new EventLifecyclePolicy
            {
                Profile = profile,
                RequiredEventFields = new HashSet<Enum>(),
                RequiredSessionFields = new HashSet<Enum>
                {
                    EventSessionFieldKey.ParentEvent,
                    EventSessionFieldKey.Tenant,
                    EventSessionFieldKey.Status,
                    EventSessionFieldKey.Title
                }
            },
            ValidationProfile.SessionSchedule => new EventLifecyclePolicy
            {
                Profile = profile,
                RequiredEventFields = new HashSet<Enum>(),
                RequiredSessionFields = new HashSet<Enum>
                {
                    EventSessionFieldKey.ParentEvent,
                    EventSessionFieldKey.Tenant,
                    EventSessionFieldKey.Status,
                    EventSessionFieldKey.ScheduleStart,
                    EventSessionFieldKey.ScheduleEnd
                }
            },
            ValidationProfile.SessionPublish => new EventLifecyclePolicy
            {
                Profile = profile,
                RequiredEventFields = new HashSet<Enum>(),
                RequiredSessionFields = new HashSet<Enum>
                {
                    EventSessionFieldKey.ParentEvent,
                    EventSessionFieldKey.Tenant,
                    EventSessionFieldKey.Status,
                    EventSessionFieldKey.Title,
                    EventSessionFieldKey.ScheduleStart,
                    EventSessionFieldKey.ScheduleEnd,
                    EventSessionFieldKey.ParentEventCompatibility
                }
            },
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unknown validation profile.")
        };
    }
}
