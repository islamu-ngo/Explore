// ABOUTME: Generated-contract-ready event participation read DTO.
// ABOUTME: Carries normalized lookup facts, scalar recovery policy, and concurrency metadata without legacy flags.

using Explore.Domain.Enums;

namespace Explore.Application.DTOs.Event;

public sealed class EventParticipationConfigurationDto
{
    public Guid EventId { get; set; }
    public Guid ConcurrencyStamp { get; set; }

    public int ParticipationHandlingModeId { get; set; }
    public string? ParticipationHandlingModeCode { get; set; }
    public string? ParticipationHandlingModeName { get; set; }

    public int AdvanceRegistrationObligationId { get; set; }
    public string? AdvanceRegistrationObligationCode { get; set; }
    public string? AdvanceRegistrationObligationName { get; set; }

    public int? IdentityAccessModeId { get; set; }
    public string? IdentityAccessModeCode { get; set; }
    public string? IdentityAccessModeName { get; set; }

    public GuestRecoveryPolicyEnum? GuestRecoveryPolicy { get; set; }
}
