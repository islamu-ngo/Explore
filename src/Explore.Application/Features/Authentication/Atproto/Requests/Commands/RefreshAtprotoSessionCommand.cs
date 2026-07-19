// ABOUTME: Requests refresh of the current authenticated tenant/user/DID-bound ATProto session.
// ABOUTME: Carries no provider credentials because Infrastructure restores them from encrypted storage.

using Explore.Application.Features.Authentication.Atproto.Models;
using MediatR;

namespace Explore.Application.Features.Authentication.Atproto.Requests.Commands;

public sealed record RefreshAtprotoSessionCommand(AtprotoCurrentSessionIdentity Identity)
    : IRequest<AtprotoSessionRefreshResult>;
