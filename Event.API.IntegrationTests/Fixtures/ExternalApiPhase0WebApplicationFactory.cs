// ABOUTME: Test host factory for the Phase 0 external API access seam.
// ABOUTME: Provides deterministic JWT validation, API-key config, and tenant lookup stubs for split-phase tenant tests.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Explore.API.Authentication;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Domain.Constants;
using Explore.Infrastructure;
using Explore.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Event.Api.IntegrationTests.Fixtures;

public sealed class ExternalApiPhase0WebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestIssuer = "https://phase0-auth.test";

    private const string JwtSigningKey = "phase0-test-signing-key-12345678901234567890";
    private readonly string _databaseName = $"InMemoryDbForExternalApiPhase0_{Guid.NewGuid():N}";

    public DeploymentMode DeploymentMode { get; init; } = DeploymentMode.MultiTenant;

    public Guid DefaultTenantId { get; init; } = PlatformDefaults.DefaultTenantId;

    public IReadOnlyDictionary<string, Guid> TenantSlugMappings { get; init; } = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, Guid> TenantDomainMappings { get; init; } = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<ApiKeyClientDescriptor> ApiKeyClients { get; init; } = [];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var inMemoryConfig = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=explore_db_test;Username=postgres;Password=postgres",
                ["Keycloak:Authority"] = TestIssuer,
                ["Keycloak:Realm"] = "explore",
                ["Keycloak:Audience"] = "explore-api",
                ["Keycloak:RequireHttpsMetadata"] = "false",
                ["Keycloak:MetadataAddress"] = $"{TestIssuer}/.well-known/openid-configuration",
                ["S3Settings:Region"] = "us-east-1",
                ["S3Settings:BucketName"] = "test-bucket",
                ["S3Settings:AccessKeyId"] = "test-key",
                ["S3Settings:SecretAccessKey"] = "test-secret",
                ["S3Settings:Endpoint"] = "https://s3.example.com",
                ["Deployment:Mode"] = DeploymentMode.ToString(),
                ["Deployment:DefaultTenantId"] = DefaultTenantId.ToString(),
                ["Authentication:ApiKeys:HeaderName"] = "X-API-Key"
            };

            for (var index = 0; index < ApiKeyClients.Count; index++)
            {
                var client = ApiKeyClients[index];
                var prefix = $"Authentication:ApiKeys:Clients:{index}";

                inMemoryConfig[$"{prefix}:KeyId"] = client.KeyId;
                inMemoryConfig[$"{prefix}:TenantId"] = client.TenantId.ToString();
                inMemoryConfig[$"{prefix}:OwnerType"] = client.OwnerType;
                inMemoryConfig[$"{prefix}:OwnerId"] = client.OwnerId;
                inMemoryConfig[$"{prefix}:IsActive"] = client.IsActive.ToString();
                inMemoryConfig[$"{prefix}:ExpiresAtUtc"] = client.ExpiresAtUtc?.ToString("O");
                inMemoryConfig[$"{prefix}:SecretHash"] = client.SecretHash;

                for (var scopeIndex = 0; scopeIndex < client.Scopes.Count; scopeIndex++)
                {
                    inMemoryConfig[$"{prefix}:Scopes:{scopeIndex}"] = client.Scopes[scopeIndex];
                }
            }

            config.AddInMemoryCollection(inMemoryConfig);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ExploreDbContext>>();

            services.AddDbContext<ExploreDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ITenantSlugCache>();
            services.AddSingleton<ITenantSlugCache>(new TestTenantSlugCache(TenantSlugMappings, TenantDomainMappings));

            services.RemoveAll<IResolverConfigService>();
            services.AddSingleton<IResolverConfigService>(new TestResolverConfigService());

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSigningKey));

                options.Authority = TestIssuer;
                options.MetadataAddress = string.Empty;
                options.RequireHttpsMetadata = false;
                options.Configuration = new OpenIdConnectConfiguration
                {
                    Issuer = TestIssuer
                };
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(options.Configuration);
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = TestIssuer,
                    ValidateAudience = true,
                    ValidAudience = "explore-api",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    NameClaimType = ClaimTypes.NameIdentifier
                };
                options.Events = new JwtBearerEvents();
            });
        });
    }

    public string CreateJwt(Guid userId, IEnumerable<Claim>? additionalClaims = null)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new("sub", userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, "Phase0 User")
        };

        if (additionalClaims is not null)
        {
            claims.AddRange(additionalClaims);
        }

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: "explore-api",
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class TestResolverConfigService : IResolverConfigService
    {
        private static readonly ResolverConfigurationDto Configuration = new()
        {
            HeaderEnabled = true,
            PathEnabled = true,
            SubdomainEnabled = false,
            CustomDomainEnabled = false,
            AllowTenantCustomDomains = false,
            InstanceBaseDomain = string.Empty
        };

        public Task<ResolverConfigurationDto> GetConfigurationAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Configuration);
        }

        public Task ApplyConfigurationAsync(ResolverConfigurationDto configuration, Guid? actorUserId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void InvalidateCache()
        {
        }
    }

    private sealed class TestTenantSlugCache : ITenantSlugCache
    {
        private readonly IReadOnlyDictionary<string, Guid> _slugMappings;
        private readonly IReadOnlyDictionary<string, Guid> _domainMappings;

        public TestTenantSlugCache(IReadOnlyDictionary<string, Guid> slugMappings, IReadOnlyDictionary<string, Guid> domainMappings)
        {
            _slugMappings = slugMappings;
            _domainMappings = domainMappings;
        }

        public Task WarmAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public ValueTask<Guid?> GetTenantIdBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(_slugMappings.TryGetValue(slug, out var tenantId) ? (Guid?)tenantId : null);
        }

        public ValueTask<Guid?> GetTenantIdByDomainAsync(string domain, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(_domainMappings.TryGetValue(domain, out var tenantId) ? (Guid?)tenantId : null);
        }
    }
}
