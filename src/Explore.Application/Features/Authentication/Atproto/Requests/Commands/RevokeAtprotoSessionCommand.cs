// ABOUTME: Requests best-effort remote revocation of the authenticated user's current ATProto session.
// ABOUTME: Carries only the server-derived tenant/user/DID tuple and returns a bounded outcome.

using Explore.Application.Features.Authentication.Atproto.Models;
using MediatR;

namespace Explore.Application.Features.Authentication.Atproto.Requests.Commands;

public sealed record RevokeAtprotoSessionCommand(AtprotoCurrentSessionIdentity Identity)
    : IRequest<AtprotoSessionRevocationResult>;
