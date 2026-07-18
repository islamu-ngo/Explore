// ABOUTME: Tests idempotent self-service revocation for user authentication sessions.
// ABOUTME: Ensures session deletion uses the current user scope and never exposes token ownership.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.UserAuthenticationTokens.Handlers.Commands;
using Explore.Application.Features.UserAuthenticationTokens.Requests.Commands;
using Explore.Domain;
using NSubstitute;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.UserAuthenticationTokens.Commands;

public class UserAuthenticationTokenCommandHandlerTests
{
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUserAuthenticationTokenRepository _repository = Substitute.For<IUserAuthenticationTokenRepository>();

    [Test]
    public async Task DeleteHandler_UsesCurrentUserScopedLookupBeforeDeleting()
    {
        var currentUserId = Guid.NewGuid();
        var token = CreateToken(currentUserId);
        _currentUserService.UserId.Returns(currentUserId);
        _repository.GetByIdForUser(token.Id, currentUserId, Arg.Any<CancellationToken>())
            .Returns(token);
        var handler = new DeleteUserAuthenticationTokenCommandHandler(
            _repository,
            _currentUserService);

        await handler.Handle(
            new DeleteUserAuthenticationTokenCommand { Id = token.Id },
            CancellationToken.None);

        await _repository.Received(1).GetByIdForUser(
            token.Id,
            currentUserId,
            Arg.Any<CancellationToken>());
        await _repository.Received(1).Delete(token);
        await _repository.DidNotReceiveWithAnyArgs().GetById(default);
    }

    [Test]
    public async Task DeleteHandler_WhenSessionIsAbsent_RemainsIdempotent()
    {
        var currentUserId = Guid.NewGuid();
        var tokenId = Guid.NewGuid();
        _currentUserService.UserId.Returns(currentUserId);
        _repository.GetByIdForUser(tokenId, currentUserId, Arg.Any<CancellationToken>())
            .Returns((UserAuthenticationToken?)null);
        var handler = new DeleteUserAuthenticationTokenCommandHandler(
            _repository,
            _currentUserService);

        await handler.Handle(
            new DeleteUserAuthenticationTokenCommand { Id = tokenId },
            CancellationToken.None);

        await _repository.Received(1).GetByIdForUser(
            tokenId,
            currentUserId,
            Arg.Any<CancellationToken>());
        await _repository.DidNotReceiveWithAnyArgs().Delete(default!);
    }

    private static UserAuthenticationToken CreateToken(Guid userId)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            User = null!,
            TenantId = Guid.NewGuid(),
            Tenant = null!,
            Provider = "atproto",
            SubjectDid = "did:plc:test",
            SessionCiphertext = [1],
            EncryptionKeyId = "encryption-key",
            OAuthClientKeyId = "oauth-client-key"
        };
}
