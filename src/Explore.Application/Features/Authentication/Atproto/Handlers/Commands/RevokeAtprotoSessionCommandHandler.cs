// ABOUTME: Revokes one authenticated tenant/user/DID-scoped ATProto OAuth session through Infrastructure.
// ABOUTME: Manually validates the server-derived identity before any credential or network access.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.Authentication.Atproto.Models;
using Explore.Application.Features.Authentication.Atproto.Requests.Commands;
using Explore.Application.Features.Authentication.Atproto.Validators;
using FluentValidation;
using MediatR;

namespace Explore.Application.Features.Authentication.Atproto.Handlers.Commands;

public sealed class RevokeAtprotoSessionCommandHandler(IAtprotoOAuthSecurityGateway securityGateway)
    : IRequestHandler<RevokeAtprotoSessionCommand, AtprotoSessionRevocationResult>
{
    public async Task<AtprotoSessionRevocationResult> Handle(
        RevokeAtprotoSessionCommand request,
        CancellationToken cancellationToken)
    {
        await new AtprotoCurrentSessionIdentityValidator()
            .ValidateAndThrowAsync(request.Identity, cancellationToken).ConfigureAwait(false);
        return await securityGateway
            .RevokeCurrentAsync(request.Identity, cancellationToken).ConfigureAwait(false);
    }
}
