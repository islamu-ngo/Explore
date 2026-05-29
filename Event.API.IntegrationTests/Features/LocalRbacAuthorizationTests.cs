// ABOUTME: Tests verifying FallbackAuthorizationService (Local RBAC) works correctly when the operator
// ABOUTME: chooses "local" authorization mode during onboarding. Uses real JWT auth + mocked role resolution
// ABOUTME: to exercise the full RuntimeAuthorizationProvider → FallbackAuthorizationService pipeline.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Text;
using Event.Api.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Event.Api.IntegrationTests.Features;

/// <summary>
/// Validates that <see cref="Explore.Infrastructure.Services.FallbackAuthorizationService"/>
/// (Local RBAC) functions correctly as a standalone authorization provider.
///
/// <para>The RuntimeAuthorizationProvider routes to FallbackAuthorizationService when
/// <c>authorization.provider</c> is <c>"local"</c> (or unset).</para>
///
/// <para>Uses real Keycloak JWTs for authentication and NSubstitute mocks for
/// IAdminContext/ITenantContext to control which role each test persona has,
/// since test users in Keycloak don't have matching domain state
/// (PlatformUserRoles, tenant role grants, etc.) in the InMemory DB.</para>
/// </summary>
[Category(TestCategories.Security)]
[ClassDataSource<KeycloakOnlyFixture>(Shared = SharedType.PerAssembly)]
public class LocalRbacAuthorizationTests : IAsyncDisposable
{
    private readonly KeycloakOnlyFixture _keycloak;

    private readonly WebApplicationFactory<Program> _instanceAdminFactory;
    private readonly HttpClient _instanceAdminClient;

    private readonly WebApplicationFactory<Program> _tenantAdminFactory;
    private readonly HttpClient _tenantAdminClient;

    private readonly WebApplicationFactory<Program> _regularUserFactory;
    private readonly HttpClient _regularUserClient;

    private readonly WebApplicationFactory<Program> _anonymousFactory;
    private readonly HttpClient _anonymousClient;

    private readonly IAdminContext _instanceAdminContext;
    private readonly IAdminContext _tenantAdminContext;
    private readonly IAdminContext _regularUserContext;
    private readonly ITenantContext _tenantContext;
    private readonly ICerbosConfigResolver _cerbosConfigResolver;

    private static readonly Guid DefaultTenantId = PlatformDefaults.DefaultTenantId;

    public LocalRbacAuthorizationTests(KeycloakOnlyFixture keycloak)
    {
        _keycloak = keycloak;

        _instanceAdminContext = CreateInstanceAdminContext();
        _tenantAdminContext = CreateTenantAdminContext();
        _regularUserContext = CreateRegularUserContext();
        _tenantContext = CreateTenantContext();
        _cerbosConfigResolver = CreateCerbosConfigResolver();

        _instanceAdminFactory = new LocalRbacWebApplicationFactory(
            keycloak.Authority, keycloak.MetadataAddress,
            _instanceAdminContext, _tenantContext, _cerbosConfigResolver);
        _instanceAdminClient = _instanceAdminFactory.CreateClient();

        _tenantAdminFactory = new LocalRbacWebApplicationFactory(
            keycloak.Authority, keycloak.MetadataAddress,
            _tenantAdminContext, _tenantContext, _cerbosConfigResolver);
        _tenantAdminClient = _tenantAdminFactory.CreateClient();

        _regularUserFactory = new LocalRbacWebApplicationFactory(
            keycloak.Authority, keycloak.MetadataAddress,
            _regularUserContext, _tenantContext, _cerbosConfigResolver);
        _regularUserClient = _regularUserFactory.CreateClient();

        _anonymousFactory = new LocalRbacWebApplicationFactory(
            keycloak.Authority, keycloak.MetadataAddress,
            _regularUserContext, _tenantContext, _cerbosConfigResolver);
        _anonymousClient = _anonymousFactory.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        _instanceAdminClient.Dispose();
        _tenantAdminClient.Dispose();
        _regularUserClient.Dispose();
        _anonymousClient.Dispose();
        await _instanceAdminFactory.DisposeAsync();
        await _tenantAdminFactory.DisposeAsync();
        await _regularUserFactory.DisposeAsync();
        await _anonymousFactory.DisposeAsync();
    }

    #region Instance Admin — Full Access

    [Test]
    public async Task LocalRbac_InstanceAdmin_GetInstanceSettings()
    {
        var token = await _keycloak.TokenClient.GetAdminTokenAsync();
        using var request = CreateAuthorizedRequest(HttpMethod.Get, "/api/instance/settings/modules", token);

        var response = await _instanceAdminClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "instance admin should have full access to instance settings via local RBAC");
    }

    [Test]
    public async Task LocalRbac_InstanceAdmin_CreateTenant()
    {
        var token = await _keycloak.TokenClient.GetAdminTokenAsync();
        using var request = CreateAuthorizedRequest(HttpMethod.Post, "/api/tenant", token, CreateTenantJson());

        var response = await _instanceAdminClient.SendAsync(request);

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.BadRequest },
            "instance admin should be allowed to create tenants via local RBAC " +
            "(actual status depends on request body validation, not authorization)");
    }

    #endregion

    #region Regular User — Restricted Access

    [Test]
    public async Task LocalRbac_RegularUser_DeniedInstanceSettings()
    {
        var token = await _keycloak.TokenClient.GetUserTokenAsync();
        using var request = CreateAuthorizedRequest(HttpMethod.Get, "/api/instance/settings/modules", token);

        var response = await _regularUserClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "regular user should be denied access to instance settings via local RBAC");
    }

    [Test]
    public async Task LocalRbac_RegularUser_DeniedTenantCreation()
    {
        var token = await _keycloak.TokenClient.GetUserTokenAsync();
        using var request = CreateAuthorizedRequest(HttpMethod.Post, "/api/tenant", token, CreateTenantJson());

        var response = await _regularUserClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "regular user should be denied tenant creation via local RBAC");
    }

    #endregion

    #region Tenant Admin — Own Tenant Access

    [Test]
    public async Task LocalRbac_TenantAdmin_CanViewTenants()
    {
        var token = await _keycloak.TokenClient.GetTenantAdminTokenAsync();
        using var request = CreateAuthorizedRequest(HttpMethod.Get, "/api/tenant", token);

        var response = await _tenantAdminClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "tenant admin should be allowed to view tenants via local RBAC");
    }

    [Test]
    public async Task LocalRbac_TenantAdmin_DeniedInstanceSettings()
    {
        var token = await _keycloak.TokenClient.GetTenantAdminTokenAsync();
        using var request = CreateAuthorizedRequest(HttpMethod.Get, "/api/instance/settings/modules", token);

        var response = await _tenantAdminClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "tenant admin should be denied access to instance settings via local RBAC");
    }

    #endregion

    #region Authentication Layer Independence

    [Test]
    public async Task LocalRbac_Anonymous_StillGets401()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/event/my");

        var response = await _anonymousClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "authentication (JWT validation) is independent of authorization (local RBAC)");
    }

    [Test]
    public async Task LocalRbac_AnonymousEndpoints_StillWork()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/eventformat");

        var response = await _anonymousClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "[AllowAnonymous] endpoints bypass both authentication and authorization");
    }

    #endregion

    #region Setting Access

    [Test]
    public async Task LocalRbac_InstanceAdmin_CanUpdateSettings()
    {
        var token = await _keycloak.TokenClient.GetAdminTokenAsync();
        using var request = CreateAuthorizedRequest(HttpMethod.Put, "/api/instance/settings/modules", token, ModuleSettingsJson);

        var response = await _instanceAdminClient.SendAsync(request);

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NotFound },
            "instance admin should be allowed to update settings via local RBAC " +
            "(actual status depends on whether the setting exists, not authorization)");
    }

    [Test]
    public async Task LocalRbac_RegularUser_DeniedSettingUpdate()
    {
        var token = await _keycloak.TokenClient.GetUserTokenAsync();
        using var request = CreateAuthorizedRequest(HttpMethod.Put, "/api/instance/settings/modules", token, ModuleSettingsJson);

        var response = await _regularUserClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "regular user should be denied instance setting update via local RBAC");
    }

    #endregion

    #region Helpers

    private const string ModuleSettingsJson = "{\"enableIslamicModule\":true,\"enableTechModule\":true}";

    private static string CreateTenantJson()
    {
        var suffix = Guid.NewGuid().ToString("N");
        return $"{{\"fullName\":\"Security Test Tenant {suffix}\",\"slug\":\"security-test-tenant-{suffix}\",\"isActive\":true}}";
    }

    private static HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string url, string token, string? jsonBody = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (jsonBody is not null)
        {
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        }

        return request;
    }

    private static IAdminContext CreateInstanceAdminContext()
    {
        var adminContext = Substitute.For<IAdminContext>();
        adminContext.UserId.Returns(Guid.NewGuid());
        adminContext.ResolveUserIdAsync(Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());
        adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        adminContext.IsInstanceAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        adminContext.IsTenantAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        adminContext.IsOrganizationAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        adminContext.GetAdminTenantIdsAsync(Arg.Any<CancellationToken>()).Returns(
            new List<Guid> { DefaultTenantId }.AsReadOnly());
        adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns(
            new List<Guid>().AsReadOnly());
        return adminContext;
    }

    private static IAdminContext CreateTenantAdminContext()
    {
        var adminContext = Substitute.For<IAdminContext>();
        adminContext.UserId.Returns(Guid.NewGuid());
        adminContext.ResolveUserIdAsync(Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());
        adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        adminContext.IsInstanceAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        adminContext.IsTenantAdminAsync(DefaultTenantId, Arg.Any<CancellationToken>()).Returns(true);
        adminContext.IsTenantAdminAsync(Arg.Is<Guid>(id => id != DefaultTenantId), Arg.Any<CancellationToken>()).Returns(false);
        adminContext.IsOrganizationAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        adminContext.GetAdminTenantIdsAsync(Arg.Any<CancellationToken>()).Returns(
            new List<Guid> { DefaultTenantId }.AsReadOnly());
        adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns(
            new List<Guid>().AsReadOnly());
        return adminContext;
    }

    private static IAdminContext CreateRegularUserContext()
    {
        var adminContext = Substitute.For<IAdminContext>();
        adminContext.UserId.Returns(Guid.NewGuid());
        adminContext.ResolveUserIdAsync(Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());
        adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        adminContext.IsInstanceAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        adminContext.IsTenantAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        adminContext.IsOrganizationAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        adminContext.GetAdminTenantIdsAsync(Arg.Any<CancellationToken>()).Returns(
            new List<Guid>().AsReadOnly());
        adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns(
            new List<Guid>().AsReadOnly());
        return adminContext;
    }

    private static ITenantContext CreateTenantContext()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(DefaultTenantId);
        return tenantContext;
    }

    private static ICerbosConfigResolver CreateCerbosConfigResolver()
    {
        var resolver = Substitute.For<ICerbosConfigResolver>();
        resolver.ResolveAsync(Arg.Any<CancellationToken>()).Returns((CerbosConfiguration?)null);
        return resolver;
    }

    #endregion

    #region WebApplicationFactory

    /// <summary>
    /// WebApplicationFactory configured for Local RBAC authorization.
    /// Sets authorization.provider = "local" (or leaves unset, which defaults to local).
    /// Uses real Keycloak JWTs for authentication and NSubstitute mocks for
    /// IAdminContext/ITenantContext/ICerbosConfigResolver to control which role
    /// each test persona has and ensure no BYO Cerbos override is attempted.
    /// </summary>
    private sealed class LocalRbacWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _keycloakAuthority;
        private readonly string _keycloakMetadataAddress;
        private readonly IAdminContext _adminContext;
        private readonly ITenantContext _tenantContext;
        private readonly ICerbosConfigResolver _cerbosConfigResolver;

        public LocalRbacWebApplicationFactory(
            string keycloakAuthority,
            string keycloakMetadataAddress,
            IAdminContext adminContext,
            ITenantContext tenantContext,
            ICerbosConfigResolver cerbosConfigResolver)
        {
            _keycloakAuthority = keycloakAuthority;
            _keycloakMetadataAddress = keycloakMetadataAddress;
            _adminContext = adminContext;
            _tenantContext = tenantContext;
            _cerbosConfigResolver = cerbosConfigResolver;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                var testConfig = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test_local_rbac;Username=postgres;Password=postgres",
                    ["Keycloak:Authority"] = _keycloakAuthority,
                    ["Keycloak:Realm"] = KeycloakContainerFixture.RealmName,
                    ["Keycloak:Audience"] = "islamu-event-api",
                    ["Keycloak:RequireHttpsMetadata"] = "false",
                    ["Keycloak:MetadataAddress"] = _keycloakMetadataAddress,
                    ["S3Settings:Region"] = "us-east-1",
                    ["S3Settings:BucketName"] = "test-bucket",
                    ["S3Settings:AccessKeyId"] = "test-key",
                    ["S3Settings:SecretAccessKey"] = "test-secret",
                    ["S3Settings:Endpoint"] = "https://s3.example.com",
                    ["Deployment:Mode"] = "SingleTenant",
                    ["Deployment:DefaultTenantId"] = DefaultTenantId.ToString(),
                    ["Testing:HostProfile"] = TestHostProfile.Security,
                    ["Cerbos:GrpcEndpoint"] = "http://localhost:19999",
                    ["Cerbos:PlaintextMode"] = "true",
                };

                config.AddInMemoryCollection(testConfig);
            });

            builder.ConfigureServices(services =>
            {
            services.RemoveExploreDbContextRegistrations();

                services.AddInMemoryExploreDbContext($"LocalRbacDb_{Guid.NewGuid():N}");

                services.RemoveAll<IDistributedCache>();
                services.AddDistributedMemoryCache();

                services.RemoveAll<IAdminContext>();
                services.AddScoped(_ => _adminContext);

                services.RemoveAll<ITenantContext>();
                services.AddScoped(_ => _tenantContext);

                services.RemoveAll<ICerbosConfigResolver>();
                services.AddScoped(_ => _cerbosConfigResolver);

                services.AddSingleton<IHostedService, LocalRbacSystemSettingSeeder>();
            });

            builder.ConfigureTestServices(services =>
            {
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.Authority = _keycloakAuthority;
                    options.MetadataAddress = _keycloakMetadataAddress;
                    options.TokenValidationParameters.ValidIssuer = _keycloakAuthority;
                    options.TokenValidationParameters.ValidIssuers = [_keycloakAuthority];
                    options.BackchannelHttpHandler = new SocketsHttpHandler
                    {
                        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                        SslOptions = new SslClientAuthenticationOptions
                        {
                            RemoteCertificateValidationCallback = (_, _, _, _) => true
                        },
                        ConnectCallback = async (context, cancellationToken) =>
                        {
                            var socket = new System.Net.Sockets.Socket(
                                System.Net.Sockets.AddressFamily.InterNetwork,
                                System.Net.Sockets.SocketType.Stream,
                                System.Net.Sockets.ProtocolType.Tcp);
                            try
                            {
                                await socket.ConnectAsync(context.DnsEndPoint, cancellationToken);
                                return new System.Net.Sockets.NetworkStream(socket, ownsSocket: true);
                            }
                            catch
                            {
                                socket.Dispose();
                                throw;
                            }
                        }
                    };
                });
            });
        }
    }

    /// <summary>
    /// Seeds the authorization.provider = "local" SystemSetting into the InMemory DB
    /// so RuntimeAuthorizationProvider routes to FallbackAuthorizationService.
    /// Runs as a hosted service after the app starts.
    /// </summary>
    private sealed class LocalRbacSystemSettingSeeder : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public LocalRbacSystemSettingSeeder(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();

            dbContext.SystemSettings.Add(new SystemSetting
            {
                Id = Guid.NewGuid(),
                SettingKey = GovernanceSettingKeys.Security.AuthorizationProvider,
                Value = "\"local\"",
                ValueType = SettingValueType.String,
                IsLocked = false,
                Category = "Security",
                Description = "Authorization provider (local RBAC)",
                DisplayOrder = 0,
                CreatedAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    #endregion
}
