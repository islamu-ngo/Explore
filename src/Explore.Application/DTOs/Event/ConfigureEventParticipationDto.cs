// ABOUTME: Explicit write contract for configuring an event's typed participation policy.
// ABOUTME: Keeps route identity and concurrency outside the generated participation payload.

using Explore.Domain.Enums;

namespace Explore.Application.DTOs.Event;

public sealed class ConfigureEventParticipationDto
{
    public int ParticipationHandlingModeId { get; set; }
    public int AdvanceRegistrationObligationId { get; set; }
    public int? IdentityAccessModeId { get; set; }
    public GuestRecoveryPolicyEnum? GuestRecoveryPolicy { get; set; }
}
