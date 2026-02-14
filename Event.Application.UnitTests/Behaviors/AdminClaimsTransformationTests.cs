// ABOUTME: Unit tests for AdminClaimsTransformation IClaimsTransformation implementation.
// Verifies DB-first admin authority claims are correctly added to the ClaimsPrincipal.

using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Identity;
using Explore.Infrastructure.Identity;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Event.Application.UnitTests.Behaviors;

public class AdminClaimsTransformationTests
{
    private readonly IAdminContext _adminContext;
    private readonly ILogger<AdminClaimsTransformation> _logger;
    private readonly AdminClaimsTransformation _sut;

    public AdminClaimsTransformationTests()
    {
        _adminContext = Substitute.For<IAdminContext>();
        _logger = Substitute.For<ILogger<AdminClaimsTransformation>>();
        _sut = new AdminClaimsTransformation(_adminContext, _logger);
    }

    [Test]
    public async Task TransformAsync_UnauthenticatedUser_ReturnsPrincipalUnchanged()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert
        await Assert.That(result).IsEqualTo(principal);
        await Assert.That(result.Identity!.IsAuthenticated).IsFalse();
    }

    [Test]
    public async Task TransformAsync_AuthenticatedUser_NullUserId_ReturnsPrincipalUnchanged()
    {
        // Arrange
        var identity = new ClaimsIdentity(new[] { new Claim("sub", "test") }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _adminContext.UserId.Returns((Guid?)null);

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert
        await Assert.That(result.HasClaim(c => c.Type == AdminClaimTypes.InstanceAdmin)).IsFalse();
        await Assert.That(result.HasClaim(c => c.Type == AdminClaimTypes.TenantAdmin)).IsFalse();
        await Assert.That(result.HasClaim(c => c.Type == AdminClaimTypes.OrganizationAdmin)).IsFalse();
    }

    [Test]
    public async Task TransformAsync_AuthenticatedUser_NoAdminAuthority_NoClaimsAdded()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var identity = new ClaimsIdentity(new[] { new Claim("sub", userId.ToString()) }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.GetAdminTenantIdsAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Guid>().ToList().AsReadOnly() as IReadOnlyList<Guid>);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Guid>().ToList().AsReadOnly() as IReadOnlyList<Guid>);

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert
        await Assert.That(result.HasClaim(c => c.Type == AdminClaimTypes.InstanceAdmin)).IsFalse();
        await Assert.That(result.HasClaim(c => c.Type == AdminClaimTypes.TenantAdmin)).IsFalse();
        await Assert.That(result.HasClaim(c => c.Type == AdminClaimTypes.OrganizationAdmin)).IsFalse();
    }

    [Test]
    public async Task TransformAsync_InstanceAdmin_AddsInstanceAdminClaim()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var identity = new ClaimsIdentity(new[] { new Claim("sub", userId.ToString()) }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        _adminContext.GetAdminTenantIdsAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Guid>().ToList().AsReadOnly() as IReadOnlyList<Guid>);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Guid>().ToList().AsReadOnly() as IReadOnlyList<Guid>);

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert
        await Assert.That(result.HasClaim(AdminClaimTypes.InstanceAdmin, "true")).IsTrue();
        await Assert.That(result.HasClaim(c => c.Type == AdminClaimTypes.TenantAdmin)).IsFalse();
        await Assert.That(result.HasClaim(c => c.Type == AdminClaimTypes.OrganizationAdmin)).IsFalse();
    }

    [Test]
    public async Task TransformAsync_TenantAdmin_AddsTenantAdminClaimsPerTenant()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId1 = Guid.NewGuid();
        var tenantId2 = Guid.NewGuid();
        var identity = new ClaimsIdentity(new[] { new Claim("sub", userId.ToString()) }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.GetAdminTenantIdsAsync(Arg.Any<CancellationToken>()).Returns(new List<Guid> { tenantId1, tenantId2 }.AsReadOnly() as IReadOnlyList<Guid>);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Guid>().ToList().AsReadOnly() as IReadOnlyList<Guid>);

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert
        await Assert.That(result.HasClaim(AdminClaimTypes.TenantAdmin, tenantId1.ToString())).IsTrue();
        await Assert.That(result.HasClaim(AdminClaimTypes.TenantAdmin, tenantId2.ToString())).IsTrue();
        var tenantClaims = result.FindAll(c => c.Type == AdminClaimTypes.TenantAdmin);
        await Assert.That(tenantClaims.Count()).IsEqualTo(2);
    }

    [Test]
    public async Task TransformAsync_OrganizationAdmin_AddsOrganizationAdminClaimsPerOrg()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orgId1 = Guid.NewGuid();
        var orgId2 = Guid.NewGuid();
        var orgId3 = Guid.NewGuid();
        var identity = new ClaimsIdentity(new[] { new Claim("sub", userId.ToString()) }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.GetAdminTenantIdsAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Guid>().ToList().AsReadOnly() as IReadOnlyList<Guid>);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns(new List<Guid> { orgId1, orgId2, orgId3 }.AsReadOnly() as IReadOnlyList<Guid>);

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert
        await Assert.That(result.HasClaim(AdminClaimTypes.OrganizationAdmin, orgId1.ToString())).IsTrue();
        await Assert.That(result.HasClaim(AdminClaimTypes.OrganizationAdmin, orgId2.ToString())).IsTrue();
        await Assert.That(result.HasClaim(AdminClaimTypes.OrganizationAdmin, orgId3.ToString())).IsTrue();
        var orgClaims = result.FindAll(c => c.Type == AdminClaimTypes.OrganizationAdmin);
        await Assert.That(orgClaims.Count()).IsEqualTo(3);
    }

    [Test]
    public async Task TransformAsync_FullAdmin_AddsAllAdminClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var identity = new ClaimsIdentity(new[] { new Claim("sub", userId.ToString()) }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        _adminContext.GetAdminTenantIdsAsync(Arg.Any<CancellationToken>()).Returns(new List<Guid> { tenantId }.AsReadOnly() as IReadOnlyList<Guid>);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns(new List<Guid> { orgId }.AsReadOnly() as IReadOnlyList<Guid>);

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert
        await Assert.That(result.HasClaim(AdminClaimTypes.InstanceAdmin, "true")).IsTrue();
        await Assert.That(result.HasClaim(AdminClaimTypes.TenantAdmin, tenantId.ToString())).IsTrue();
        await Assert.That(result.HasClaim(AdminClaimTypes.OrganizationAdmin, orgId.ToString())).IsTrue();
    }

    [Test]
    public async Task TransformAsync_AlreadyHasAdminClaims_SkipsTransformation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("sub", userId.ToString()),
            new Claim(AdminClaimTypes.InstanceAdmin, "true")
        }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _adminContext.UserId.Returns(userId);

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert — IAdminContext should never be called since claims already exist
        await _adminContext.DidNotReceive().IsInstanceAdminAsync(Arg.Any<CancellationToken>());
        await _adminContext.DidNotReceive().GetAdminTenantIdsAsync(Arg.Any<CancellationToken>());
        await _adminContext.DidNotReceive().GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>());
        // Only the original claim should exist (no duplicates)
        var instanceClaims = result.FindAll(c => c.Type == AdminClaimTypes.InstanceAdmin);
        await Assert.That(instanceClaims.Count()).IsEqualTo(1);
    }

    [Test]
    public async Task TransformAsync_AlreadyHasTenantAdminClaim_SkipsTransformation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("sub", userId.ToString()),
            new Claim(AdminClaimTypes.TenantAdmin, tenantId.ToString())
        }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _adminContext.UserId.Returns(userId);

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert — IAdminContext should never be called since claims already exist
        await _adminContext.DidNotReceive().IsInstanceAdminAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TransformAsync_AlreadyHasOrganizationAdminClaim_SkipsTransformation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("sub", userId.ToString()),
            new Claim(AdminClaimTypes.OrganizationAdmin, orgId.ToString())
        }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _adminContext.UserId.Returns(userId);

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert — IAdminContext should never be called since claims already exist
        await _adminContext.DidNotReceive().IsInstanceAdminAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TransformAsync_AdminContextThrowsException_FailsOpenAndLogWarning()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var identity = new ClaimsIdentity(new[] { new Claim("sub", userId.ToString()) }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("DB connection failed"));

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert — should return principal without admin claims (fail-open)
        await Assert.That(result.HasClaim(c => c.Type == AdminClaimTypes.InstanceAdmin)).IsFalse();
        await Assert.That(result.HasClaim(c => c.Type == AdminClaimTypes.TenantAdmin)).IsFalse();
        await Assert.That(result.HasClaim(c => c.Type == AdminClaimTypes.OrganizationAdmin)).IsFalse();

        // Verify warning was logged
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Is<InvalidOperationException>(ex => ex.Message == "DB connection failed"),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public async Task TransformAsync_InstanceAdminOnly_DoesNotAddTenantOrOrgClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var identity = new ClaimsIdentity(new[] { new Claim("sub", userId.ToString()) }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        _adminContext.GetAdminTenantIdsAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Guid>().ToList().AsReadOnly() as IReadOnlyList<Guid>);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Guid>().ToList().AsReadOnly() as IReadOnlyList<Guid>);

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert
        var allAdminClaims = result.FindAll(c =>
            c.Type == AdminClaimTypes.InstanceAdmin
            || c.Type == AdminClaimTypes.TenantAdmin
            || c.Type == AdminClaimTypes.OrganizationAdmin);
        await Assert.That(allAdminClaims.Count()).IsEqualTo(1);
        await Assert.That(allAdminClaims.First().Type).IsEqualTo(AdminClaimTypes.InstanceAdmin);
    }

    [Test]
    public async Task TransformAsync_CallsAllAdminContextMethods()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var identity = new ClaimsIdentity(new[] { new Claim("sub", userId.ToString()) }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.GetAdminTenantIdsAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Guid>().ToList().AsReadOnly() as IReadOnlyList<Guid>);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Guid>().ToList().AsReadOnly() as IReadOnlyList<Guid>);

        // Act
        await _sut.TransformAsync(principal);

        // Assert — all three admin resolution methods should be called
        await _adminContext.Received(1).IsInstanceAdminAsync(Arg.Any<CancellationToken>());
        await _adminContext.Received(1).GetAdminTenantIdsAsync(Arg.Any<CancellationToken>());
        await _adminContext.Received(1).GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>());
    }
}
