using Options = Microsoft.Extensions.Options.Options;

namespace Explore.Blazor.Client.Tests.Services;

/// <summary>
/// Unit tests for AuthStateService.
/// Tests authentication state management and claim extraction.
/// </summary>
/// <remarks>
/// <para>
/// Key design decisions tested:
/// - GetCurrentUserIdAsync returns string (JWT claims are strings)
/// - GetCurrentTenantIdAsync returns Guid (parsed from string claims)
/// - Fallback chain for user ID: sub → nameidentifier → sid (AGENTS.md rule #8)
/// - Multi-tenant vs single-tenant mode behavior
/// </para>
/// <para>
/// The userId is stored as Guid in the domain model (User.Id) but returned as string
/// from GetCurrentUserIdAsync because JWT claims are inherently strings.
/// </para>
/// </remarks>
public class AuthStateServiceTests
{
    #region GetCurrentUserIdAsync Tests

    [Test]
    public async Task GetCurrentUserIdAsync_ReturnsNameIdentifierClaim_WhenPresent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        var logger = Substitute.For<ILogger<AuthStateService>>();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(principal);
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));

        var config = new TenantConfiguration
        {
            Enabled = false,
            DefaultTenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001"),
            DefaultTenant = "test"
        };
        var service = new AuthStateService(authStateProvider, logger, Options.Create(config));

        // Act
        var result = await service.GetCurrentUserIdAsync();

        // Assert - Result is string, but represents a Guid
        await Assert.That(result).IsEqualTo(userId.ToString());
        await Assert.That(Guid.TryParse(result, out _)).IsTrue();
    }

    [Test]
    public async Task GetCurrentUserIdAsync_FallsBackToSubClaim_WhenNameIdentifierNotPresent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        var logger = Substitute.For<ILogger<AuthStateService>>();
        var claims = new[] { new Claim("sub", userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(principal);
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));

        var config = new TenantConfiguration
        {
            Enabled = false,
            DefaultTenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001"),
            DefaultTenant = "test"
        };
        var service = new AuthStateService(authStateProvider, logger, Options.Create(config));

        // Act
        var result = await service.GetCurrentUserIdAsync();

        // Assert
        await Assert.That(result).IsEqualTo(userId.ToString());
    }

    [Test]
    public async Task GetCurrentUserIdAsync_PrefersSubClaim_WhenBothSubAndNameIdentifierPresent()
    {
        // Arrange — both sub and nameidentifier present; sub must win per AGENTS.md rule #8
        var subUserId = Guid.NewGuid();
        var nameIdUserId = Guid.NewGuid();
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        var logger = Substitute.For<ILogger<AuthStateService>>();
        var claims = new[]
        {
            new Claim("sub", subUserId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, nameIdUserId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(principal);
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));

        var config = new TenantConfiguration
        {
            Enabled = false,
            DefaultTenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001"),
            DefaultTenant = "test"
        };
        var service = new AuthStateService(authStateProvider, logger, Options.Create(config));

        // Act
        var result = await service.GetCurrentUserIdAsync();

        // Assert — sub claim takes priority over nameidentifier
        await Assert.That(result).IsEqualTo(subUserId.ToString());
        await Assert.That(result).IsNotEqualTo(nameIdUserId.ToString());
    }

    [Test]
    public async Task GetCurrentUserIdAsync_FallsBackToSidClaim_WhenOtherClaimsNotPresent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        var logger = Substitute.For<ILogger<AuthStateService>>();
        var claims = new[] { new Claim("sid", userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(principal);
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));

        var config = new TenantConfiguration
        {
            Enabled = false,
            DefaultTenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001"),
            DefaultTenant = "test"
        };
        var service = new AuthStateService(authStateProvider, logger, Options.Create(config));

        // Act
        var result = await service.GetCurrentUserIdAsync();

        // Assert
        await Assert.That(result).IsEqualTo(userId.ToString());
    }

    [Test]
    public async Task GetCurrentUserIdAsync_ThrowsUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        var logger = Substitute.For<ILogger<AuthStateService>>();
        var identity = new ClaimsIdentity(); // No auth type = not authenticated
        var principal = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(principal);
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));

        var config = new TenantConfiguration
        {
            Enabled = false,
            DefaultTenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001"),
            DefaultTenant = "test"
        };
        var service = new AuthStateService(authStateProvider, logger, Options.Create(config));

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await service.GetCurrentUserIdAsync());
    }

    [Test]
    public async Task GetCurrentUserIdAsync_ThrowsUnauthorized_WhenNoUserIdClaimPresent()
    {
        // Arrange
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        var logger = Substitute.For<ILogger<AuthStateService>>();
        // Authenticated but no user ID claims
        var claims = new[] { new Claim(ClaimTypes.Email, "test@example.com") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(principal);
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));

        var config = new TenantConfiguration
        {
            Enabled = false,
            DefaultTenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001"),
            DefaultTenant = "test"
        };
        var service = new AuthStateService(authStateProvider, logger, Options.Create(config));

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await service.GetCurrentUserIdAsync());
    }

    #endregion

    #region GetCurrentTenantIdAsync Tests

    [Test]
    public async Task GetCurrentTenantIdAsync_ReturnsDefaultTenantId_WhenSingleTenantMode()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var defaultTenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        var logger = Substitute.For<ILogger<AuthStateService>>();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(principal);
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));

        // Single-tenant mode (Enabled = false)
        var config = new TenantConfiguration
        {
            Enabled = false,
            DefaultTenantId = defaultTenantId,
            DefaultTenant = "test"
        };
        var service = new AuthStateService(authStateProvider, logger, Options.Create(config));

        // Act
        var result = await service.GetCurrentTenantIdAsync();

        // Assert - Returns Guid (not string)
        await Assert.That(result).IsEqualTo(defaultTenantId);
    }

    [Test]
    public async Task GetCurrentTenantIdAsync_ReturnsTenantIdFromClaims_WhenMultiTenantMode()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        var logger = Substitute.For<ILogger<AuthStateService>>();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("tenant_id", tenantId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(principal);
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));

        // Multi-tenant mode (Enabled = true)
        var config = new TenantConfiguration
        {
            Enabled = true,
            DefaultTenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001"),
            DefaultTenant = "test"
        };
        var service = new AuthStateService(authStateProvider, logger, Options.Create(config));

        // Act
        var result = await service.GetCurrentTenantIdAsync();

        // Assert
        await Assert.That(result).IsEqualTo(tenantId);
    }

    [Test]
    public async Task GetCurrentTenantIdAsync_ThrowsInvalidOperation_WhenMultiTenantModeAndNoTenantClaim()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        var logger = Substitute.For<ILogger<AuthStateService>>();
        // No tenant_id claim
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(principal);
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));

        // Multi-tenant mode requires tenant claim
        var config = new TenantConfiguration
        {
            Enabled = true,
            DefaultTenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001"),
            DefaultTenant = "test"
        };
        var service = new AuthStateService(authStateProvider, logger, Options.Create(config));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.GetCurrentTenantIdAsync());
    }

    [Test]
    public async Task GetCurrentTenantIdAsync_ThrowsUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        var logger = Substitute.For<ILogger<AuthStateService>>();
        var identity = new ClaimsIdentity(); // Not authenticated
        var principal = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(principal);
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));

        var config = new TenantConfiguration
        {
            Enabled = false,
            DefaultTenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001"),
            DefaultTenant = "test"
        };
        var service = new AuthStateService(authStateProvider, logger, Options.Create(config));

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await service.GetCurrentTenantIdAsync());
    }

    [Test]
    public async Task GetCurrentTenantIdAsync_SupportsAlternativeTenantClaimNames()
    {
        // Arrange - Using "tid" claim (Azure AD style)
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        var logger = Substitute.For<ILogger<AuthStateService>>();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("tid", tenantId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(principal);
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));

        var config = new TenantConfiguration
        {
            Enabled = true,
            DefaultTenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001"),
            DefaultTenant = "test"
        };
        var service = new AuthStateService(authStateProvider, logger, Options.Create(config));

        // Act
        var result = await service.GetCurrentTenantIdAsync();

        // Assert
        await Assert.That(result).IsEqualTo(tenantId);
    }

    #endregion

    #region IsAuthenticatedAsync Tests

    [Test]
    public async Task IsAuthenticatedAsync_ReturnsTrue_WhenUserAuthenticated()
    {
        // Arrange
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        var logger = Substitute.For<ILogger<AuthStateService>>();
        var identity = new ClaimsIdentity(new[] { new Claim("sub", "test") }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(principal);
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));

        var config = new TenantConfiguration
        {
            Enabled = false,
            DefaultTenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001"),
            DefaultTenant = "test"
        };
        var service = new AuthStateService(authStateProvider, logger, Options.Create(config));

        // Act
        var result = await service.IsAuthenticatedAsync();

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAuthenticatedAsync_ReturnsFalse_WhenUserNotAuthenticated()
    {
        // Arrange
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        var logger = Substitute.For<ILogger<AuthStateService>>();
        var identity = new ClaimsIdentity(); // No auth type = not authenticated
        var principal = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(principal);
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));

        var config = new TenantConfiguration
        {
            Enabled = false,
            DefaultTenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001"),
            DefaultTenant = "test"
        };
        var service = new AuthStateService(authStateProvider, logger, Options.Create(config));

        // Act
        var result = await service.IsAuthenticatedAsync();

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsAuthenticatedAsync_ReturnsFalse_WhenIdentityIsNull()
    {
        // Arrange
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        var logger = Substitute.For<ILogger<AuthStateService>>();
        var principal = new ClaimsPrincipal(); // No identity
        var authState = new AuthenticationState(principal);
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));

        var config = new TenantConfiguration
        {
            Enabled = false,
            DefaultTenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001"),
            DefaultTenant = "test"
        };
        var service = new AuthStateService(authStateProvider, logger, Options.Create(config));

        // Act
        var result = await service.IsAuthenticatedAsync();

        // Assert
        await Assert.That(result).IsFalse();
    }

    #endregion
}
