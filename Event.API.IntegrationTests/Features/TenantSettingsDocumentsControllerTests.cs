// ABOUTME: Integration tests for tenant typed settings document endpoints.
// ABOUTME: Covers authentication gates and authorized HAL responses for branding document endpoints.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using Explore.Application.DTOs.TenantSettingsDocuments;
using Explore.Application.Features.TenantSettingsDocuments.Requests.Commands;
using Explore.Application.Features.TenantSettingsDocuments.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

[NotInParallel("ApiTestFixture")]
[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public sealed class TenantSettingsDocumentsControllerAnonymousTests
{
    private readonly ApiTestFixture _fixture;

    public TenantSettingsDocumentsControllerAnonymousTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task GetBranding_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.GetAsync("/api/tenant/settings/documents/branding");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ReplaceBranding_WithoutAuth_ShouldReturnUnauthorized()
    {
        var request = new ReplaceTenantBrandingSettingsDocumentDto
        {
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            Payload = new TenantBrandingSettingsPayloadDto
            {
                DisplayName = "Unauthenticated Brand"
            }
        };

        var response = await _fixture.Client.PutAsJsonAsync("/api/tenant/settings/documents/branding", request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}

public sealed class TenantSettingsDocumentsControllerAuthorizedTests
{
    [Test]
    public async Task GetBranding_WithAuth_ShouldReturnHalDocument()
    {
        var documentId = Guid.NewGuid();
        using var factory = CreateFactoryWithMediator(new BrandingDocumentMediator(documentId));
        using var client = factory.CreateClient();
        using var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/tenant/settings/documents/branding");

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        await Assert.That(root.GetProperty("documentKey").GetString()).IsEqualTo("tenant.branding");
        await Assert.That(root.GetProperty("payload").GetProperty("displayName").GetString()).IsEqualTo("Typed Tenant");
        await Assert.That(root.GetProperty("_links").TryGetProperty("self", out _)).IsTrue();
        await Assert.That(root.GetProperty("_links").TryGetProperty("self/replace-settings", out _)).IsTrue();
    }

    [Test]
    public async Task ReplaceBranding_WithAuth_ShouldSendCommandAndReturnUpdatedHalDocument()
    {
        var documentId = Guid.NewGuid();
        var initialStamp = Guid.NewGuid();
        var mediator = new BrandingDocumentMediator(documentId, initialStamp);
        using var factory = CreateFactoryWithMediator(mediator);
        using var client = factory.CreateClient();
        using var request = CreateAuthenticatedRequest(HttpMethod.Put, "/api/tenant/settings/documents/branding");
        request.Content = JsonContent.Create(new ReplaceTenantBrandingSettingsDocumentDto
        {
            ExpectedConcurrencyStamp = initialStamp,
            Payload = new TenantBrandingSettingsPayloadDto
            {
                DisplayName = "Updated Tenant",
                LogoUrl = "https://cdn.example.test/logo.svg",
                FaviconUrl = "https://cdn.example.test/favicon.ico",
                CustomCssUrl = "https://cdn.example.test/tenant.css"
            }
        });

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(mediator.LastReplaceCommand).IsNotNull();
        await Assert.That(mediator.LastReplaceCommand!.Document.ExpectedConcurrencyStamp).IsEqualTo(initialStamp);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        await Assert.That(root.GetProperty("payload").GetProperty("displayName").GetString()).IsEqualTo("Updated Tenant");
        await Assert.That(root.GetProperty("payload").GetProperty("customCssUrl").GetString()).IsEqualTo("https://cdn.example.test/tenant.css");
        await Assert.That(root.GetProperty("_links").TryGetProperty("self/replace-settings", out _)).IsTrue();
    }

    private static WebApplicationFactory<Program> CreateFactoryWithMediator(IMediator mediator)
    {
        var factory = new AuthenticatedWebApplicationFactory
        {
            AuthorizationProviderOverride = new StubAuthorizationProvider { AllowAll = true }
        };

        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IMediator>();
                services.AddSingleton(mediator);
            });
        });
    }

    private static HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add(TestAuthHandler.AuthHeaderName, TestAuthHandler.CreateAuthHeaderValue(Guid.NewGuid()));
        return request;
    }

    private sealed class BrandingDocumentMediator : IMediator
    {
        private readonly Guid _documentId;
        private TenantBrandingSettingsDocumentDto _document;

        public BrandingDocumentMediator(Guid documentId, Guid? concurrencyStamp = null)
        {
            _documentId = documentId;
            _document = CreateDocument(
                displayName: "Typed Tenant",
                logoUrl: "https://cdn.example.test/logo.svg",
                faviconUrl: "https://cdn.example.test/favicon.ico",
                customCssUrl: "https://cdn.example.test/tenant.css",
                concurrencyStamp: concurrencyStamp ?? Guid.NewGuid());
        }

        public ReplaceTenantBrandingSettingsDocumentCommand? LastReplaceCommand { get; private set; }

        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
            => Task.CompletedTask;

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            object? response = request switch
            {
                GetTenantBrandingSettingsDocumentQuery => _document,
                ReplaceTenantBrandingSettingsDocumentCommand command => Replace(command),
                _ => throw new InvalidOperationException($"Unexpected request type {request.GetType().Name}.")
            };

            return Task.FromResult((TResponse)response);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
            => Task.CompletedTask;

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => request switch
            {
                GetTenantBrandingSettingsDocumentQuery => Task.FromResult<object?>(_document),
                ReplaceTenantBrandingSettingsDocumentCommand command => Task.FromResult<object?>(Replace(command)),
                _ => throw new InvalidOperationException($"Unexpected request type {request.GetType().Name}.")
            };

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        private BaseCommandResponse<Guid> Replace(ReplaceTenantBrandingSettingsDocumentCommand command)
        {
            LastReplaceCommand = command;
            _document = CreateDocument(
                command.Document.Payload.DisplayName,
                command.Document.Payload.LogoUrl,
                command.Document.Payload.FaviconUrl,
                command.Document.Payload.CustomCssUrl,
                Guid.NewGuid());

            return new BaseCommandResponse<Guid>
            {
                Success = true,
                Message = "Tenant branding settings document replaced.",
                Id = _documentId
            };
        }

        private TenantBrandingSettingsDocumentDto CreateDocument(
            string? displayName,
            string? logoUrl,
            string? faviconUrl,
            string? customCssUrl,
            Guid concurrencyStamp)
            => new()
            {
                DocumentKey = "tenant.branding",
                SchemaVersion = 1,
                DefaultsVersion = "2026-05-14",
                Payload = new TenantBrandingSettingsPayloadDto
                {
                    DisplayName = displayName,
                    LogoUrl = logoUrl,
                    FaviconUrl = faviconUrl,
                    CustomCssUrl = customCssUrl
                },
                Source = "tenant",
                SourceScopeId = _documentId,
                ConcurrencyStamp = concurrencyStamp,
                UpdatedAt = DateTime.UtcNow
            };
    }
}
