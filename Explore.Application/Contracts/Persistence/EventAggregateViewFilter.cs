// ABOUTME: Persistence-neutral filter contract for EventWithSessions aggregate read-model repository queries.
// ABOUTME: Keeps repository inputs decoupled from DTOs while preserving current list filtering semantics.

namespace Explore.Application.Contracts.Persistence;

public sealed record EventAggregateViewFilter(
    string? Title,
    DateTimeOffset? StartAtFrom,
    DateTimeOffset? StartAtTo,
    string? Status,
    string? Visibility);
