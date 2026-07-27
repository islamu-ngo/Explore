// ABOUTME: Requests independent PDS verification and local session bootstrap for one ATProto identity.
// ABOUTME: Receives only server-private bridge material after bootstrap assertion authentication.

using Explore.Application.Features.Authentication.Atproto.Models;
using MediatR;

namespace Explore.Application.Features.Authentication.Atproto.Requests.Commands;

public sealed record BootstrapAtprotoSessionCommand(
    string ExpectedDid,
    string ExpectedPdsUri,
    string OAuthClientKeyId,
    AtprotoSubjectClassification Classification,
    byte[] OAuthSessionPayload,
    Guid? CanonicalActorId = null,
    Guid? ExpectedCanonicalActorConcurrencyStamp = null) : IRequest<AtprotoSessionBootstrapResult>;
