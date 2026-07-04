// ABOUTME: Tests self-service ownership enforcement for authentication token command handlers.
// ABOUTME: Ensures token writes stamp user and tenant from trusted server context.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.UserAuthenticationToken;
using Explore.Application.Features.UserAuthenticationTokens.Handlers.Commands;
using Explore.Application.Features.UserAuthenticationTokens.Requests.Commands;
using Explore.Domain;
using NSubstitute;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.UserAuthenticationTokens.Commands;

public class UserAuthenticationTokenCommandHandlerTests
{
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IUserAuthenticationTokenRepository _repository = Substitute.For<IUserAuthenticationTokenRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();

    [Test]
    public async Task CreateHandler_StampsCurrentUserAndTenantBeforePersisting()
    {
        var currentUserId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var dto = CreateDto();
        var mapped = CreateToken(Guid.Empty, Guid.Empty, dto.Provider);
        _currentUserService.UserId.Returns(currentUserId);
        _tenantContext.TenantId.Returns(tenantId);
        _mapper.Map<UserAuthenticationToken>(dto).Returns(mapped);
        _repository.Create(Arg.Any<UserAuthenticationToken>()).Returns(call =>
        {
            var token = call.Arg<UserAuthenticationToken>();
            token.Id = Guid.NewGuid();
            return token;
        });
        var handler = new CreateUserAuthenticationTokenCommandHandler(
            _repository,
            _tenantContext,
            _currentUserService,
            _mapper);

        var result = await handler.Handle(
            new CreateUserAuthenticationTokenCommand { UserAuthenticationTokenDto = dto },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _repository.Received(1).Create(Arg.Is<UserAuthenticationToken>(token =>
            token.UserId == currentUserId &&
            token.TenantId == tenantId &&
            token.Provider == dto.Provider));
    }

    [Test]
    public async Task UpdateHandler_UsesCurrentUserScopedLookupBeforeUpdating()
    {
        var currentUserId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var dto = UpdateDto();
        var existing = CreateToken(currentUserId, tenantId, "old-provider");
        existing.Id = dto.Id;
        _currentUserService.UserId.Returns(currentUserId);
        _repository.GetByIdForUser(dto.Id, currentUserId, Arg.Any<CancellationToken>())
            .Returns(existing);
        var handler = new UpdateUserAuthenticationTokenCommandHandler(
            _repository,
            _currentUserService,
            _mapper);

        var result = await handler.Handle(
            new UpdateUserAuthenticationTokenCommand { UserAuthenticationTokenDto = dto },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _repository.Received(1).GetByIdForUser(
            dto.Id,
            currentUserId,
            Arg.Any<CancellationToken>());
        await _repository.Received(1).Update(existing);
        await _repository.DidNotReceiveWithAnyArgs().GetById(default);
    }

    [Test]
    public async Task DeleteHandler_UsesCurrentUserScopedLookupBeforeDeleting()
    {
        var currentUserId = Guid.NewGuid();
        var token = CreateToken(currentUserId, Guid.NewGuid(), "atproto");
        _currentUserService.UserId.Returns(currentUserId);
        _repository.GetByIdForUser(token.Id, currentUserId, Arg.Any<CancellationToken>())
            .Returns(token);
        var handler = new DeleteUserAuthenticationTokenCommandHandler(
            _repository,
            _currentUserService);

        var result = await handler.Handle(
            new DeleteUserAuthenticationTokenCommand { Id = token.Id },
            CancellationToken.None);

        await Assert.That(result).IsTrue();
        await _repository.Received(1).GetByIdForUser(
            token.Id,
            currentUserId,
            Arg.Any<CancellationToken>());
        await _repository.Received(1).Delete(token);
        await _repository.DidNotReceiveWithAnyArgs().GetById(default);
    }

    private static CreateUserAuthenticationTokenDto CreateDto()
        => new()
        {
            Provider = "atproto",
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            PdsHost = "https://pds.example.test",
            DpopKey = "dpop-key",
            IdToken = "id-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

    private static UpdateUserAuthenticationTokenDto UpdateDto()
        => new()
        {
            Id = Guid.NewGuid(),
            Provider = "atproto",
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            PdsHost = "https://pds.example.test",
            DpopKey = "dpop-key",
            IdToken = "id-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

    private static UserAuthenticationToken CreateToken(Guid userId, Guid tenantId, string provider)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            User = null!,
            TenantId = tenantId,
            Tenant = null!,
            Provider = provider
        };
}
