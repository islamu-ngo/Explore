// ABOUTME: Generated-contract-ready event participation read DTO.
// ABOUTME: Carries normalized lookup facts, scalar recovery policy, and concurrency metadata without legacy flags.

using System.Text.Json.Serialization;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.Event;

public sealed record EventParticipationConfigurationDto
{
    public Guid EventId { get; init; }
    public Guid ConcurrencyStamp { get; init; }

    public int ParticipationHandlingModeId { get; init; }
    public string? ParticipationHandlingModeCode { get; init; }
    public string? ParticipationHandlingModeName { get; init; }

    public int AdvanceRegistrationObligationId { get; init; }
    public string? AdvanceRegistrationObligationCode { get; init; }
    public string? AdvanceRegistrationObligationName { get; init; }

    public int? IdentityAccessModeId { get; init; }
    public string? IdentityAccessModeCode { get; init; }
    public string? IdentityAccessModeName { get; init; }

    public GuestRecoveryPolicyEnum? GuestRecoveryPolicy { get; init; }

    [JsonIgnore]
    public bool HasValidOptionalQuestionnaire { get; init; }
}
