// ABOUTME: Defines the CarpaNet-free boundary for validated ATProto event record payloads.
// ABOUTME: Returns exact immutable JSON and SHA-256 only after lexicon and encoded-size validation pass.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Federation.Atproto.Models;

namespace Explore.Application.Contracts.Infrastructure;

public static class RepositoryBackedAtprotoSession
{
    public const string Provider = "atproto";
}

public sealed record AtprotoPublicationPayload(string Json, string Sha256);

public sealed record AtprotoPublicationPayloadBuildResult(
    AtprotoPublicationPayload? Payload,
    string? FailureCode)
{
    public bool IsValid => Payload is not null && FailureCode is null;

    public static AtprotoPublicationPayloadBuildResult Valid(AtprotoPublicationPayload payload) =>
        new(payload, null);

    public static AtprotoPublicationPayloadBuildResult Invalid(string failureCode) =>
        new(null, failureCode);
}

public interface IAtprotoPublicationPayloadBuilder
{
    Task<AtprotoPublicationPayloadBuildResult> BuildEventAsync(
        AtprotoEventPublicationEntityGraph graph,
        DateTimeOffset serverNowUtc,
        CancellationToken cancellationToken);
}
