// ABOUTME: Carries the bounded community-calendar fields accepted for one inbound federated event import.
// ABOUTME: Keeps lexicon content validation separate from tenant and canonical record identity.

namespace Explore.Application.Features.Federation.Atproto.Models;

public sealed record AtprotoFederatedEventImportInput(
    string Name,
    DateTimeOffset? CreatedAt)
{
    public string? Description { get; init; }
    public string? SourceUrl { get; init; }
    public DateTimeOffset? StartsAt { get; init; }
    public DateTimeOffset? EndsAt { get; init; }
    public string? Mode { get; init; }
    public string? Status { get; init; }
    public bool? RsvpExpected { get; init; }
}
