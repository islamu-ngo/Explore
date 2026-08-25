// ABOUTME: Requests independent PDS verification and local session bootstrap for one ATProto identity.
// ABOUTME: Receives only server-private bridge material after bootstrap assertion authentication.

using Explore.Application.Features.Authentication.Atproto.Models;
using MediatR;

namespace Explore.Application.Features.Authentication.Atproto.Requests.Commands;

public sealed record BootstrapAtprotoSessionCommand : IRequest<AtprotoSessionBootstrapResult>
{
    public BootstrapAtprotoSessionCommand(
        string ExpectedDid,
        string ExpectedPdsUri,
        string OAuthClientKeyId,
        AtprotoSubjectClassification Classification,
        ReadOnlyMemory<byte> OAuthSessionPayload,
        Guid? CanonicalActorId = null,
        Guid? ExpectedCanonicalActorConcurrencyStamp = null)
    {
        this.ExpectedDid = ExpectedDid;
        this.ExpectedPdsUri = ExpectedPdsUri;
        this.OAuthClientKeyId = OAuthClientKeyId;
        this.Classification = Classification;
        this.OAuthSessionPayload = OAuthSessionPayload.ToArray();
        this.CanonicalActorId = CanonicalActorId;
        this.ExpectedCanonicalActorConcurrencyStamp = ExpectedCanonicalActorConcurrencyStamp;
    }

    public string ExpectedDid { get; }
    public string ExpectedPdsUri { get; }
    public string OAuthClientKeyId { get; }
    public AtprotoSubjectClassification Classification { get; }
    public ReadOnlyMemory<byte> OAuthSessionPayload { get; }
    public Guid? CanonicalActorId { get; }
    public Guid? ExpectedCanonicalActorConcurrencyStamp { get; }
}
