using System.Net;
using Event.Api.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

public class EventMultiTagFilterTests : IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly Guid _tenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");

    public EventMultiTagFilterTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
        services.RemoveExploreDbContextRegistrations();
                services.AddDbContext<ExploreDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"InMemoryDb_{Guid.NewGuid()}");
                });

                // Mock ITenantSlugCache to always resolve our tenant
                services.RemoveAll<ITenantSlugCache>();
                services.AddSingleton<ITenantSlugCache>(new TestTenantSlugCache(_tenantId));
            });
        });
        _client = _factory.CreateClient();
    }

    private class TestTenantSlugCache : ITenantSlugCache
    {
        private readonly Guid _tenantId;
        public TestTenantSlugCache(Guid tenantId) => _tenantId = tenantId;
        public ValueTask<Guid?> GetTenantIdBySlugAsync(string slug, CancellationToken ct = default) => new(_tenantId);
        public ValueTask<Guid?> GetTenantIdByDomainAsync(string domain, CancellationToken ct = default) => new(_tenantId);
        public Task WarmAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RefreshAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task EnsureTenantExistsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();

        if (!dbContext.Tenants.Any(t => t.Id == _tenantId))
        {
            var activeStatus = new TenantStatus
            {
                Id = (int)TenantStatusEnum.Active,
                MasterCode = "ACTIVE",
                FullName = "Active",
                IsActiveState = true
            };

            dbContext.Tenants.Add(new Tenant
            {
                Id = _tenantId,
                FullName = "Default Tenant",
                Slug = "default",
                TenantStatusId = (int)TenantStatusEnum.Active,
                TenantStatus = activeStatus
            });
            await dbContext.SaveChangesAsync();
        }
    }

    private async Task<HttpResponseMessage> GetAsyncWithTenant(string url)
    {
        await EnsureTenantExistsAsync();
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Tenant-Slug", "default");
        return await _client.SendAsync(request);
    }

    private async Task AssertSuccessAsync(HttpResponseMessage response, string url)
    {
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[TEST DEBUG] URL: {url}, Status: {response.StatusCode}, Content: {content}");
        }
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetEvents_WithMultiTagFilters_ShouldReturnOk()
    {
        // Arrange
        var tag1 = Guid.NewGuid();
        var tag2 = Guid.NewGuid();
        var url = $"/api/event?includedTagIds={tag1}&includedTagIds={tag2}&inclusionMode=and&excludedTagIds={Guid.NewGuid()}&exclusionMode=or";

        // Act
        var response = await GetAsyncWithTenant(url);

        // Assert
        await AssertSuccessAsync(response, url);
    }

    [Test]
    public async Task GetEvents_WithIncludedTags_OrMode_ShouldReturnOk()
    {
        // Arrange
        var tag1 = Guid.NewGuid();
        var url = $"/api/event?includedTagIds={tag1}&inclusionMode=or";

        // Act
        var response = await GetAsyncWithTenant(url);

        // Assert
        await AssertSuccessAsync(response, url);
    }

    [Test]
    public async Task GetEvents_WithExcludedTags_AndMode_ShouldReturnOk()
    {
        // Arrange
        var tag1 = Guid.NewGuid();
        var url = $"/api/event?excludedTagIds={tag1}&exclusionMode=and";

        // Act
        var response = await GetAsyncWithTenant(url);

        // Assert
        await AssertSuccessAsync(response, url);
    }
}
