// ABOUTME: Tests delete behavior for external-login unlinking including last-provider safety.
// ABOUTME: Verifies not-found, blocked-last-provider, and successful delete paths.

using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Application.Features.UserExternalLogins.Handlers.Commands;
using Explore.Application.Features.UserExternalLogins.Requests.Commands;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.UserExternalLogins.Commands;

public class DeleteUserExternalLoginCommandHandlerTests
{
    private readonly IUserExternalLoginRepository _userExternalLoginRepository;
    private readonly DeleteUserExternalLoginCommandHandler _handler;

    public DeleteUserExternalLoginCommandHandlerTests()
    {
        _userExternalLoginRepository = Substitute.For<IUserExternalLoginRepository>();
        _handler = new DeleteUserExternalLoginCommandHandler(_userExternalLoginRepository);
    }

    [Test]
    public async Task Handle_ReturnsFalse_WhenLoginDoesNotExist()
    {
        var loginId = Guid.NewGuid();
        _userExternalLoginRepository.GetById(loginId).Returns((UserExternalLogin?)null);

        var result = await _handler.Handle(new DeleteUserExternalLoginCommand { Id = loginId }, CancellationToken.None);

        await Assert.That(result).IsFalse();
        await _userExternalLoginRepository.DidNotReceive().Delete(Arg.Any<UserExternalLogin>());
    }

    [Test]
    public async Task Handle_ThrowsBadRequest_WhenTryingToUnlinkLastProvider()
    {
        var userId = Guid.NewGuid();
        var login = BuildLogin(userId, Guid.NewGuid(), "google", "google-sub-123");

        _userExternalLoginRepository.GetById(login.Id).Returns(login);
        _userExternalLoginRepository.GetByUser(userId).Returns([login]);

        await Assert.ThrowsAsync<BadRequestException>(
            async () => await _handler.Handle(new DeleteUserExternalLoginCommand { Id = login.Id }, CancellationToken.None));

        await _userExternalLoginRepository.DidNotReceive().Delete(Arg.Any<UserExternalLogin>());
    }

    [Test]
    public async Task Handle_DeletesLogin_WhenUserHasMoreThanOneProvider()
    {
        var userId = Guid.NewGuid();
        var loginToDelete = BuildLogin(userId, Guid.NewGuid(), "google", "google-sub-123");
        var secondLogin = BuildLogin(userId, Guid.NewGuid(), "keycloak", Guid.NewGuid().ToString());

        _userExternalLoginRepository.GetById(loginToDelete.Id).Returns(loginToDelete);
        _userExternalLoginRepository.GetByUser(userId).Returns([loginToDelete, secondLogin]);

        var result = await _handler.Handle(new DeleteUserExternalLoginCommand { Id = loginToDelete.Id }, CancellationToken.None);

        await Assert.That(result).IsTrue();
        await _userExternalLoginRepository.Received(1).Delete(loginToDelete);
    }

    private static UserExternalLogin BuildLogin(Guid userId, Guid loginId, string provider, string providerKey)
    {
        var user = DataBuilder.User.Generate();
        user.Id = userId;

        var tenant = DataBuilder.Tenant.Generate();

        return new UserExternalLogin
        {
            Id = loginId,
            UserId = userId,
            User = user,
            TenantId = tenant.Id,
            Tenant = tenant,
            Provider = provider,
            ProviderKey = providerKey
        };
    }
}
