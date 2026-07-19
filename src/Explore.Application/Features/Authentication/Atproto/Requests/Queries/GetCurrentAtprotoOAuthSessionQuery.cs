// ABOUTME: Requests the current authenticated user's encrypted-at-rest ATProto OAuth session.
// ABOUTME: Carries only the identity tuple already authenticated by the private API bridge.

using Explore.Application.Features.Authentication.Atproto.Models;
using MediatR;

namespace Explore.Application.Features.Authentication.Atproto.Requests.Queries;

public sealed record GetCurrentAtprotoOAuthSessionQuery(AtprotoCurrentSessionIdentity Identity)
    : IRequest<AtprotoCurrentOAuthSession?>;
