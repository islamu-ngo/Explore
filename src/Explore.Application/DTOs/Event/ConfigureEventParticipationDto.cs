// ABOUTME: Explicit write contract for configuring an event's typed participation policy.
// ABOUTME: Keeps route identity and concurrency outside the generated participation payload.

using System.ComponentModel.DataAnnotations;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.Event;

public sealed class ConfigureEventParticipationDto
{
    [Required]
    public int ParticipationHandlingModeId { get; set; }

    [Required]
    public int AdvanceRegistrationObligationId { get; set; }

    public int? IdentityAccessModeId { get; set; }
    public GuestRecoveryPolicyEnum? GuestRecoveryPolicy { get; set; }
}
