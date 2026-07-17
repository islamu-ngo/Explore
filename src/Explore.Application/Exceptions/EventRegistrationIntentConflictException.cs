// ABOUTME: Typed application exception for exact registration-intent uniqueness races.
// ABOUTME: Allows the caller-owned transaction to roll back before loading the winning intent.

namespace Explore.Application.Exceptions;

public sealed class EventRegistrationIntentConflictException(Exception innerException)
    : Exception("An equivalent event registration intent already exists.", innerException);
