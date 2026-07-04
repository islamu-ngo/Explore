// ABOUTME: Tests self-service scoping for user authentication token read handlers.
// ABOUTME: Ensures handlers never list or detail another user's credential metadata.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.UserAuthenticationToken;
using Explore.Application.Exceptions;
using Explore.Application.Features.UserAuthenticationTokens.Handlers.Queries;
using Explore.Application.Features.UserAuthenticationTokens.Requests.Queries;
using Explore.Domain;
using NSubstitute;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.UserAuthenticationTokens.Queries;

public class UserAuthenticationTokenSelfReadHandlerTests
{
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUserAuthenticationTokenRepository _repository = Substitute.For<IUserAuthenticationTokenRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();

    [Test]
    public async Task ListHandler_UsesCurrentUserForRepositoryRead()
    {
        var currentUserId = Guid.NewGuid();
        var token = CreateToken(currentUserId);
        var dto = new UserAuthenticationTokenListDto
        {
            Id = token.Id,
            Provider = token.Provider,
            PdsHost = token.PdsHost,
            ExpiresAt = token.ExpiresAt
        };
        _currentUserService.UserId.Returns(currentUserId);
        _repository.GetUserAuthenticationTokensWithDetailsForUser(
                currentUserId,
                Arg.Any<CancellationToken>())
            .Returns(new List<UserAuthenticationToken> { token });
        _mapper.Map<List<UserAuthenticationTokenListDto>>(Arg.Any<List<UserAuthenticationToken>>())
            .Returns([dto]);
        var handler = new GetUserAuthenticationTokenListRequestHandler(
            _repository,
            _mapper,
            _currentUserService);

        var result = await handler.Handle(new GetUserAuthenticationTokenListRequest(), CancellationToken.None);

        await Assert.That(result).IsEquivalentTo(new List<UserAuthenticationTokenListDto> { dto });
        await _repository.Received(1).GetUserAuthenticationTokensWithDetailsForUser(
            currentUserId,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ListHandler_WhenCurrentUserIsMissing_ThrowsAuthorizationException()
    {
        var handler = new GetUserAuthenticationTokenListRequestHandler(
            _repository,
            _mapper,
            _currentUserService);

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            handler.Handle(new GetUserAuthenticationTokenListRequest(), CancellationToken.None));

        await _repository.DidNotReceiveWithAnyArgs().GetUserAuthenticationTokensWithDetailsForUser(
            default,
            default);
    }

    [Test]
    public async Task DetailHandler_UsesCurrentUserForRepositoryRead()
    {
        var currentUserId = Guid.NewGuid();
        var token = CreateToken(currentUserId);
        var dto = new UserAuthenticationTokenDto
        {
            Id = token.Id,
            Provider = token.Provider,
            PdsHost = token.PdsHost,
            ExpiresAt = token.ExpiresAt
        };
        _currentUserService.UserId.Returns(currentUserId);
        _repository.GetUserAuthenticationTokenWithDetailsForUser(
                token.Id,
                currentUserId,
                Arg.Any<CancellationToken>())
            .Returns(token);
        _mapper.Map<UserAuthenticationTokenDto>(token).Returns(dto);
        var handler = new GetUserAuthenticationTokenDetailsRequestHandler(
            _repository,
            _mapper,
            _currentUserService);

        var result = await handler.Handle(
            new GetUserAuthenticationTokenDetailsRequest { Id = token.Id },
            CancellationToken.None);

        await Assert.That(result).IsEquivalentTo(dto);
        await _repository.Received(1).GetUserAuthenticationTokenWithDetailsForUser(
            token.Id,
            currentUserId,
            Arg.Any<CancellationToken>());
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
            PdsHost = "https://pds.example.test",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
}
