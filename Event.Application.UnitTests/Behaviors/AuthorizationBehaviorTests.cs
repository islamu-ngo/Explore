// ABOUTME: Unit tests for AuthorizationBehavior MediatR pipeline behavior.
// ABOUTME: Verifies authorization enforcement via IAuthorizedRequest (legacy), [AuthorizeResource], and pass-through.

#pragma warning disable CS0618 // IAuthorizedRequest is obsolete — tests must still exercise the legacy code path

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
    private readonly IAuthorizationProvider _authService;
    private readonly ILogger<AuthorizationBehavior<TestAuthorizedCommand, BaseCommandResponse<Guid>>> _logger;

    public AuthorizationBehaviorTests()
    {
        _authService = Substitute.For<IAuthorizationProvider>();
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
    public async Task Handle_WithAuthorizeResourceAttribute_WhenAllowed_CallsNext()
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
    public async Task Handle_WithAuthorizeResourceAttribute_WhenDenied_ThrowsAuthorizationException()
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

    [Test]
    public async Task Handle_WithAuthorizeResourceAndISecureRequest_WhenAllowed_UsesResourceIdFromRequest()
    {
        // Arrange
        var secureBehavior = new AuthorizationBehavior<TestSecureCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<TestSecureCommand, BaseCommandResponse<Guid>>>>());
        var command = new TestSecureCommand();
        var expectedResponse = new BaseCommandResponse<Guid> { Success = true };

        _authService.IsAllowedAsync(
            "organization", command.OrganizationId.ToString(), "update",
            Arg.Any<IDictionary<string, object>?>(),
            Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await secureBehavior.Handle(command, () => Task.FromResult(expectedResponse), CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await _authService.Received(1).IsAllowedAsync(
            "organization",
            command.OrganizationId.ToString(),
            "update",
            Arg.Any<IDictionary<string, object>?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithAuthorizeResourceAndISecureRequest_WhenDenied_ThrowsAuthorizationException()
    {
        // Arrange
        var secureBehavior = new AuthorizationBehavior<TestSecureCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<TestSecureCommand, BaseCommandResponse<Guid>>>>());
        var command = new TestSecureCommand();

        _authService.IsAllowedAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<IDictionary<string, object>?>(),
            Arg.Any<CancellationToken>())
            .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<AuthorizationException>(async () =>
            await secureBehavior.Handle(command, () => Task.FromResult(new BaseCommandResponse<Guid>()), CancellationToken.None));
    }

    [Test]
    public async Task Handle_WithAuthorizeResourceAndISecureRequest_PassesResourceAttributes()
    {
        // Arrange
        var secureBehavior = new AuthorizationBehavior<TestSecureCommandWithAttributes, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<TestSecureCommandWithAttributes, BaseCommandResponse<Guid>>>>());
        var command = new TestSecureCommandWithAttributes();
        var expectedResponse = new BaseCommandResponse<Guid> { Success = true };

        _authService.IsAllowedAsync(
            "organization", command.OrganizationId.ToString(), "delete",
            Arg.Is<IDictionary<string, object>?>(d => d != null && d.ContainsKey("tenantId")),
            Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await secureBehavior.Handle(command, () => Task.FromResult(expectedResponse), CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await _authService.Received(1).IsAllowedAsync(
            "organization",
            command.OrganizationId.ToString(),
            "delete",
            Arg.Is<IDictionary<string, object>?>(d => d != null && d.ContainsKey("tenantId")),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithAuthorizeResourceAndISecureRequest_NullResourceId_FallsBackToTypeName()
    {
        // Arrange
        var secureBehavior = new AuthorizationBehavior<TestSecureCommandWithNullId, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<TestSecureCommandWithNullId, BaseCommandResponse<Guid>>>>());
        var command = new TestSecureCommandWithNullId();
        var expectedResponse = new BaseCommandResponse<Guid> { Success = true };

        _authService.IsAllowedAsync(
            "organization", nameof(TestSecureCommandWithNullId), "delete",
            Arg.Any<IDictionary<string, object>?>(),
            Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await secureBehavior.Handle(command, () => Task.FromResult(expectedResponse), CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await _authService.Received(1).IsAllowedAsync(
            "organization",
            nameof(TestSecureCommandWithNullId),
            "delete",
            Arg.Any<IDictionary<string, object>?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public void AuthorizeResourceAttribute_EnumConstructor_ConvertsActionToString()
    {
        // Arrange & Act
        var attribute = new AuthorizeResourceAttribute("organization", AuthorizationActions.Update);

        // Assert
        Assert.That(attribute.Resource == "organization");
        Assert.That(attribute.Action == "update");
    }
}

// Test command implementing IAuthorizedRequest
public class TestAuthorizedCommand : IRequest<BaseCommandResponse<Guid>>, IAuthorizedRequest
{
    public string ResourceKind => "tenant_setting";
    public string ResourceId => "test-resource";
    public string Action => "update";
}

// Test command with [AuthorizeResource] attribute
[AuthorizeResource("instance_setting", "update")]
public class TestAttributeCommand : IRequest<BaseCommandResponse<Guid>>
{
}

// Test command with no authorization requirement
public class TestPlainCommand : IRequest<BaseCommandResponse<Guid>>
{
}

// Test command with [AuthorizeResource] enum + ISecureRequest providing dynamic ResourceId
[AuthorizeResource("organization", AuthorizationActions.Update)]
public class TestSecureCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid OrganizationId { get; set; } = Guid.NewGuid();

    string? ISecureRequest.ResourceId => OrganizationId.ToString();
}

// Test command with [AuthorizeResource] enum + ISecureRequest providing ResourceId and ResourceAttributes
[AuthorizeResource("organization", AuthorizationActions.Delete)]
public class TestSecureCommandWithAttributes : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid OrganizationId { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; } = Guid.NewGuid();

    string? ISecureRequest.ResourceId => OrganizationId.ToString();
    IDictionary<string, object>? ISecureRequest.ResourceAttributes =>
        new Dictionary<string, object> { ["tenantId"] = TenantId.ToString() };
}

// Test command with [AuthorizeResource] enum + ISecureRequest but null ResourceId — should fall back to type name
[AuthorizeResource("organization", AuthorizationActions.Delete)]
public class TestSecureCommandWithNullId : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    // Uses default null ResourceId — should fall back to type name
}
