// ABOUTME: Typed application exception for the one approved notification-intent deduplication constraint race.
// ABOUTME: Lets transaction owners roll back before loading the winning graph in a fresh transaction.

namespace Explore.Application.Exceptions;

public sealed class NotificationIntentDeduplicationConflictException(Exception innerException)
    : Exception("A notification intent with the same tenant deduplication key already exists.", innerException);
