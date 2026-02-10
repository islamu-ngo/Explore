// ABOUTME: Unit tests for AuthorizationBehavior MediatR pipeline behavior.
// Verifies authorization enforcement via IAuthorizedRequest, [CerbosAuthorize], and pass-through.

using Explore.Application.Authorization;
using Explore.Application.Behaviors;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Exceptions;
using Explore.Application.Responses;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Behaviors;

public class AuthorizationBehaviorTests
{
    private readonly ICerbosAuthorizationService _authService;
    private readonly ILogger<AuthorizationBehavior<TestAuthorizedCommand, BaseCommandResponse<Guid>>> _logger;

    public AuthorizationBehaviorTests()
    {
        _authService = Substitute.For<ICerbosAuthorizationService>();
        _logger = Substitute.For<ILogger<AuthorizationBehavior<TestAuthorizedCommand, BaseCommandResponse<Guid>>>>();
    }

    [Test]
    public async Task Handle_WithIAuthorizedRequest_WhenAllowed_CallsNext()
    {
        // Arrange
        var behavior = new AuthorizationBehavior<TestAuthorizedCommand, BaseCommandResponse<Guid>>(_authService, _logger);
        var command = new TestAuthorizedCommand();
        var expectedResponse = new BaseCommandResponse<Guid> { Success = true, Id = Guid.NewGuid() };

        _authService.IsAllowedAsync(
            "tenant_setting", "test-resource", "update",
            Arg.Any<IDictionary<string, object>?>(),
            Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await behavior.Handle(command, () => Task.FromResult(expectedResponse), CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(expectedResponse.Id);
    }

    [Test]
    public async Task Handle_WithIAuthorizedRequest_WhenDenied_ThrowsAuthorizationException()
    {
        // Arrange
        var behavior = new AuthorizationBehavior<TestAuthorizedCommand, BaseCommandResponse<Guid>>(_authService, _logger);
        var command = new TestAuthorizedCommand();

        _authService.IsAllowedAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<IDictionary<string, object>?>(),
            Arg.Any<CancellationToken>())
            .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<AuthorizationException>(async () =>
            await behavior.Handle(command, () => Task.FromResult(new BaseCommandResponse<Guid>()), CancellationToken.None));
    }

    [Test]
    public async Task Handle_WithCerbosAuthorizeAttribute_WhenAllowed_CallsNext()
    {
        // Arrange
        var attrBehavior = new AuthorizationBehavior<TestAttributeCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<TestAttributeCommand, BaseCommandResponse<Guid>>>>());
        var command = new TestAttributeCommand();
        var expectedResponse = new BaseCommandResponse<Guid> { Success = true };

        _authService.IsAllowedAsync(
            "instance_setting", Arg.Any<string>(), "update",
            Arg.Any<IDictionary<string, object>?>(),
            Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await attrBehavior.Handle(command, () => Task.FromResult(expectedResponse), CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
    }

    [Test]
    public async Task Handle_WithCerbosAuthorizeAttribute_WhenDenied_ThrowsAuthorizationException()
    {
        // Arrange
        var attrBehavior = new AuthorizationBehavior<TestAttributeCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<TestAttributeCommand, BaseCommandResponse<Guid>>>>());
        var command = new TestAttributeCommand();

        _authService.IsAllowedAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<IDictionary<string, object>?>(),
            Arg.Any<CancellationToken>())
            .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<AuthorizationException>(async () =>
            await attrBehavior.Handle(command, () => Task.FromResult(new BaseCommandResponse<Guid>()), CancellationToken.None));
    }

    [Test]
    public async Task Handle_WithNoAuthRequirement_PassesThrough()
    {
        // Arrange
        var plainBehavior = new AuthorizationBehavior<TestPlainCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<TestPlainCommand, BaseCommandResponse<Guid>>>>());
        var command = new TestPlainCommand();
        var expectedResponse = new BaseCommandResponse<Guid> { Success = true };

        // Act
        var result = await plainBehavior.Handle(command, () => Task.FromResult(expectedResponse), CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await _authService.DidNotReceive().IsAllowedAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<IDictionary<string, object>?>(),
            Arg.Any<CancellationToken>());
    }
}

// Test command implementing IAuthorizedRequest
public class TestAuthorizedCommand : IRequest<BaseCommandResponse<Guid>>, IAuthorizedRequest
{
    public string ResourceKind => "tenant_setting";
    public string ResourceId => "test-resource";
    public string Action => "update";
}

// Test command with [CerbosAuthorize] attribute
[CerbosAuthorize("instance_setting", "update")]
public class TestAttributeCommand : IRequest<BaseCommandResponse<Guid>>
{
}

// Test command with no authorization requirement
public class TestPlainCommand : IRequest<BaseCommandResponse<Guid>>
{
}
