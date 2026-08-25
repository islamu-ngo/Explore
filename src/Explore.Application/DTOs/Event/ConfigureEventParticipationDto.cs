// ABOUTME: Explicit write contract for configuring an event's typed participation policy.
// ABOUTME: Keeps route identity and concurrency outside the generated participation payload.

using System.ComponentModel.DataAnnotations;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.Event;

public sealed record ConfigureEventParticipationDto
{
    [Required]
    public int ParticipationHandlingModeId { get; init; }

    [Required]
    public int AdvanceRegistrationObligationId { get; init; }

    public int? IdentityAccessModeId { get; init; }
    public GuestRecoveryPolicyEnum? GuestRecoveryPolicy { get; init; }
}
