// ABOUTME: Unit tests for CerbosAuthorizationService request/response mapping and deny semantics.
// Verifies payload construction, missing-user fail-closed behavior, and HTTP error handling.

using System.Net;
using System.Net.Http;
using System.Text;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Event.Application.UnitTests.Behaviors;

public class CerbosAuthorizationServiceTests : IDisposable
{
    private readonly IAdminContext _adminContext;
    private readonly ISettingsResolver _settingsResolver;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<CerbosAuthorizationService> _logger;
    private readonly FakeHttpClientFactory _httpClientFactory;
    private readonly RecordingHandler _handler;

    public CerbosAuthorizationServiceTests()
    {
        _adminContext = Substitute.For<IAdminContext>();
        _settingsResolver = Substitute.For<ISettingsResolver>();
        _tenantContext = Substitute.For<ITenantContext>();
        _logger = Substitute.For<ILogger<CerbosAuthorizationService>>();

        _handler = new RecordingHandler();
        _httpClientFactory = new FakeHttpClientFactory(new HttpClient(_handler)
        {
            BaseAddress = new Uri("http://localhost:3592")
        });
    }

    [Test]
    public async Task IsAllowedBatchAsync_NoUserId_DeniesAllWithoutHttpCall()
    {
        _adminContext.UserId.Returns((Guid?)null);

        var service = CreateService();
        var checks = new List<AuthorizationCheck>
        {
            new("organization", "org-1", "update", null),
            new("tenant_setting", "setting-key", "read", null)
        };

        var result = await service.IsAllowedBatchAsync(checks);

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0]).IsFalse();
        await Assert.That(result[1]).IsFalse();
        await Assert.That(_handler.CallCount).IsEqualTo(0);
    }

    [Test]
    public async Task IsAllowedBatchAsync_MapsAllowAndDenyFromCerbosResponse()
    {
        var userId = Guid.NewGuid();
        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.GetAdminTenantIdsAsync(Arg.Any<CancellationToken>()).Returns([]);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns([]);

        _handler.ResponseFactory = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "results": [
                    { "actions": { "update": "EFFECT_ALLOW" } },
                    { "actions": { "delete": "EFFECT_DENY" } }
                  ]
                }
                """,
                Encoding.UTF8,
                "application/json")
        };

        var service = CreateService();
        var checks = new List<AuthorizationCheck>
        {
            new("organization", "org-1", "update", null),
            new("organization", "org-2", "delete", null)
        };

        var result = await service.IsAllowedBatchAsync(checks);

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0]).IsTrue();
        await Assert.That(result[1]).IsFalse();
        await Assert.That(_handler.CallCount).IsEqualTo(1);
        await Assert.That(_handler.LastRequestBody).Contains("\"organization\"");
        await Assert.That(_handler.LastRequestBody).Contains("\"authenticated_user\"");
    }

    [Test]
    public async Task IsAllowedBatchAsync_HttpFailure_DeniesAll()
    {
        var userId = Guid.NewGuid();
        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.GetAdminTenantIdsAsync(Arg.Any<CancellationToken>()).Returns([]);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns([]);

        _handler.ResponseFactory = _ => new HttpResponseMessage(HttpStatusCode.InternalServerError);

        var service = CreateService();
        var checks = new List<AuthorizationCheck>
        {
            new("organization", "org-1", "update", null),
            new("tenant_setting", "setting-1", "update", null)
        };

        var result = await service.IsAllowedBatchAsync(checks);

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0]).IsFalse();
        await Assert.That(result[1]).IsFalse();
    }

    [Test]
    public async Task CheckSettingAccessAsync_TenantScope_SendsLockAndTenantAttributes()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.GetAdminTenantIdsAsync(Arg.Any<CancellationToken>()).Returns([tenantId]);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns([]);
        _settingsResolver.CanOverrideAsync("events.require_approval", Arg.Any<CancellationToken>()).Returns(false);

        _handler.ResponseFactory = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "results": [
                    { "actions": { "update": "EFFECT_DENY" } }
                  ]
                }
                """,
                Encoding.UTF8,
                "application/json")
        };

        var service = CreateService();
        var allowed = await service.CheckSettingAccessAsync("events.require_approval", "update", tenantId: tenantId);

        await Assert.That(allowed).IsFalse();
        await Assert.That(_handler.LastRequestBody).Contains("\"tenant_setting\"");
        await Assert.That(_handler.LastRequestBody).Contains("\"tenantId\"");
        await Assert.That(_handler.LastRequestBody).Contains(tenantId.ToString());
        await Assert.That(_handler.LastRequestBody).Contains("\"isLockedByInstance\":true");
    }

    private CerbosAuthorizationService CreateService()
    {
        return new CerbosAuthorizationService(
            _httpClientFactory,
            new CerbosPrincipalBuilder(_adminContext),
            _adminContext,
            _settingsResolver,
            _tenantContext,
            Options.Create(new CerbosSettings { Enabled = true, Endpoint = "http://localhost:3592" }),
            _logger);
    }

    private sealed class FakeHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }
        public string LastRequestBody { get; private set; } = string.Empty;
        public Func<HttpRequestMessage, HttpResponseMessage>? ResponseFactory { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            if (ResponseFactory is not null)
            {
                return ResponseFactory(request);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"results\":[{\"actions\":{\"read\":\"EFFECT_ALLOW\"}}]}", Encoding.UTF8, "application/json")
            };
        }
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
