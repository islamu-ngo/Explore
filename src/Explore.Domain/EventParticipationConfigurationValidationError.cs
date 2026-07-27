// ABOUTME: Typed validation detail for one invalid event participation configuration rule.
// ABOUTME: Couples a machine-readable code with a human-readable Domain explanation.

using Explore.Domain.Enums;

namespace Explore.Domain;

public sealed record EventParticipationConfigurationValidationError(
    EventParticipationConfigurationErrorCode Code,
    string Message);
