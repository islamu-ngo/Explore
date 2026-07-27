// ABOUTME: Shared-primary-key event participation policy with explicit identity and recovery semantics.
// ABOUTME: Enforces legal configuration before creation or atomic reconfiguration.

using Explore.Domain.Enums;
using Explore.Domain.Interfaces;
using Explore.Domain.Services.Registration;

namespace Explore.Domain;

public sealed class EventParticipationConfiguration :
    ITenantEntity,
    IAuditableEntity,
    ISoftDeletable,
    IConcurrencyAware
{
    private EventParticipationConfiguration()
    {
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; private set; }
    public Event? Event { get; private set; }
    public int ParticipationHandlingModeId { get; private set; }
    public ParticipationHandlingMode? ParticipationHandlingMode { get; private set; }
    public int AdvanceRegistrationObligationId { get; private set; }
    public AdvanceRegistrationObligation? AdvanceRegistrationObligation { get; private set; }
    public int? IdentityAccessModeId { get; private set; }
    public IdentityAccessMode? IdentityAccessMode { get; private set; }
    public GuestRecoveryPolicyEnum? GuestRecoveryPolicy { get; private set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public Guid ConcurrencyStamp { get; set; }

    public static EventParticipationConfiguration Create(
        Guid eventId,
        Guid tenantId,
        int participationHandlingModeId,
        int advanceRegistrationObligationId,
        int? identityAccessModeId,
        GuestRecoveryPolicyEnum? guestRecoveryPolicy,
        DateTime now)
    {
        EnsureValid(
            eventId,
            tenantId,
            participationHandlingModeId,
            advanceRegistrationObligationId,
            identityAccessModeId,
            guestRecoveryPolicy);

        return new EventParticipationConfiguration
        {
            Id = eventId,
            TenantId = tenantId,
            ParticipationHandlingModeId = participationHandlingModeId,
            AdvanceRegistrationObligationId = advanceRegistrationObligationId,
            IdentityAccessModeId = identityAccessModeId,
            GuestRecoveryPolicy = guestRecoveryPolicy,
            CreatedAt = EnsureUtc(now)
        };
    }

    public void Reconfigure(
        int participationHandlingModeId,
        int advanceRegistrationObligationId,
        int? identityAccessModeId,
        GuestRecoveryPolicyEnum? guestRecoveryPolicy)
    {
        EnsureValid(
            Id,
            TenantId,
            participationHandlingModeId,
            advanceRegistrationObligationId,
            identityAccessModeId,
            guestRecoveryPolicy);

        ParticipationHandlingModeId = participationHandlingModeId;
        AdvanceRegistrationObligationId = advanceRegistrationObligationId;
        IdentityAccessModeId = identityAccessModeId;
        GuestRecoveryPolicy = guestRecoveryPolicy;
    }

    private static void EnsureValid(
        Guid eventId,
        Guid tenantId,
        int participationHandlingModeId,
        int advanceRegistrationObligationId,
        int? identityAccessModeId,
        GuestRecoveryPolicyEnum? guestRecoveryPolicy)
    {
        var errors = EventAuthorityRules.ValidateParticipationConfiguration(
            participationHandlingModeId,
            advanceRegistrationObligationId,
            identityAccessModeId,
            guestRecoveryPolicy).ToList();

        if (eventId == Guid.Empty)
        {
            errors.Add(new(
                EventParticipationConfigurationErrorCode.EventIdRequired,
                "Event id is required."));
        }

        if (tenantId == Guid.Empty)
        {
            errors.Add(new(
                EventParticipationConfigurationErrorCode.TenantIdRequired,
                "Tenant id is required."));
        }

        if (errors.Count > 0)
        {
            throw new EventParticipationConfigurationValidationException(errors);
        }
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        if (value == default || value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must be a non-default UTC value.", nameof(value));
        }

        return value;
    }
}
