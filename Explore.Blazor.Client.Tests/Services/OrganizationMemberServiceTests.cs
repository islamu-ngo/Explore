// ABOUTME: Unit tests for OrganizationMemberService covering member management, invitations,
// role updates, and delete operations with proper error handling verification.

namespace Explore.Blazor.Client.Tests.Services;

/// <summary>
/// Tests OrganizationMemberService behavior patterns:
/// - Read operations (GetMembers, GetMyInvitations) return empty collections on error
/// - Write operations (Invite, UpdateRole, Accept, Decline, Delete) throw on error
/// </summary>
public class OrganizationMemberServiceTests
{
    private readonly IEventApiClient _apiClient;
    private readonly OrganizationMemberService _service;

    public OrganizationMemberServiceTests()
    {
        _apiClient = Substitute.For<IEventApiClient>();
        var logger = Substitute.For<ILogger<OrganizationMemberService>>();
        _service = new OrganizationMemberService(_apiClient, logger);
    }

    // ========== Read Operations (return empty on error) ==========

    #region GetMembersAsync Tests

    [Test]
    public async Task GetMembersAsync_ReturnsMembers_WhenApiSucceeds()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var members = new List<OrganizationMemberDto>
         {
             new() { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), OrganizationId = organizationId },
             new() { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), OrganizationId = organizationId }
         };

        _apiClient.GetOrganizationMembersByOrganizationAsync(organizationId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(members);

        // Act
        var result = await _service.GetMembersAsync(organizationId);

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
    }

    [Test]
    public async Task GetMembersAsync_ReturnsEmptyList_WhenApiThrows()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        _apiClient.GetOrganizationMembersByOrganizationAsync(organizationId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiException("API Error", 500, null, null, null));

        // Act
        var result = await _service.GetMembersAsync(organizationId);

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetMembersAsync_ReturnsEmptyList_WhenResponseIsNull()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        _apiClient.GetOrganizationMembersByOrganizationAsync(organizationId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((ICollection<OrganizationMemberDto>?)null);

        // Act
        var result = await _service.GetMembersAsync(organizationId);

        // Assert
        await Assert.That(result).IsEmpty();
    }

    #endregion

    #region GetMyInvitationsAsync Tests

    [Test]
    public async Task GetMyInvitationsAsync_ReturnsInvitations_WhenApiSucceeds()
    {
        // Arrange
        var invitations = new List<OrganizationInvitationDto>
        {
            new() { Id = Guid.NewGuid(), OrganizationId = Guid.NewGuid(), Email = "user1@example.com" },
            new() { Id = Guid.NewGuid(), OrganizationId = Guid.NewGuid(), Email = "user2@example.com" }
        };

        _apiClient.GetMyOrganizationInvitationsAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(invitations);

        // Act
        var result = await _service.GetMyInvitationsAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
    }

    [Test]
    public async Task GetMyInvitationsAsync_ReturnsEmptyList_WhenApiThrows()
    {
        // Arrange
        _apiClient.GetMyOrganizationInvitationsAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiException("API Error", 500, null, null, null));

        // Act
        var result = await _service.GetMyInvitationsAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    #endregion

    // ========== Write Operations (throw on error) ==========

    #region InviteMemberAsync Tests

    [Test]
    public async Task InviteMemberAsync_ReturnsResponse_WhenApiSucceeds()
    {
        // Arrange
        var dto = new AddOrganizationMemberDto
        {
            OrganizationId = Guid.NewGuid(),
            Email = "test@example.com"
        };
        var expected = ComponentDataBuilder.SuccessResponse();

        _apiClient.AddOrganizationMemberAsync(dto, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(expected);

        // Act
        var result = await _service.InviteMemberAsync(dto);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
    }

    [Test]
    public async Task InviteMemberAsync_Throws_WhenApiThrows()
    {
        // Arrange
        var dto = new AddOrganizationMemberDto
        {
            OrganizationId = Guid.NewGuid(),
            Email = "test@example.com"
        };
        _apiClient.AddOrganizationMemberAsync(dto, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiException("Bad Request", 400, null, null, null));

        // Act & Assert
        await Assert.ThrowsAsync<ApiException>(async () => await _service.InviteMemberAsync(dto));
    }

    #endregion

    #region AcceptInvitationAsync Tests

    [Test]
    public async Task AcceptInvitationAsync_Throws_WhenApiThrows()
    {
        // Arrange
        var invitationId = Guid.NewGuid();
        _apiClient.AcceptOrganizationInvitationAsync(invitationId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiException("Not Found", 404, null, null, null));

        // Act & Assert
        await Assert.ThrowsAsync<ApiException>(async () => await _service.AcceptInvitationAsync(invitationId));
    }

    #endregion

    #region DeclineInvitationAsync Tests

    [Test]
    public async Task DeclineInvitationAsync_Throws_WhenApiThrows()
    {
        // Arrange
        var invitationId = Guid.NewGuid();
        _apiClient.DeclineOrganizationInvitationAsync(invitationId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiException("Not Found", 404, null, null, null));

        // Act & Assert
        await Assert.ThrowsAsync<ApiException>(async () => await _service.DeclineInvitationAsync(invitationId));
    }

    #endregion

    #region DeleteMemberAsync Tests

    [Test]
    public async Task DeleteMemberAsync_Throws_WhenApiThrows()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        _apiClient.DeleteOrganizationMemberAsync(memberId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiException("Forbidden", 403, null, null, null));

        // Act & Assert
        await Assert.ThrowsAsync<ApiException>(async () => await _service.DeleteMemberAsync(memberId));
    }

    #endregion
}
