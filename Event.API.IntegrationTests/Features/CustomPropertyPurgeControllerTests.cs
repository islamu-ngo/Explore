// ABOUTME: API contract tests for explicit audited custom-property purge endpoints.
// ABOUTME: Verifies admin-only access and dependency-blocked purge response shape without invoking normal soft delete.

using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.CustomPropertyDefinition;
using Explore.Application.Features.CustomPropertyDefinitions.Requests.Commands;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

public sealed class CustomPropertyPurgeControllerTests
{
    private const string BaseUrl = "/api/custompropertydefinition";

    [Test]
    public async Task Purge_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var factory = CreateFactoryWithMediator(new PurgeMediator());
        using var client = factory.CreateClient();
        using var request = CreatePurgeRequest(Guid.NewGuid(), authenticated: false);

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Purge_WhenDependenciesExist_ReturnsBadRequestWithAuditCounts()
    {
        var definitionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var summary = new CustomPropertyPurgeDependencySummary(
            definitionId,
            tenantId,
            "custom_property_definition",
            OptionCount: 2,
            ValueCount: 1,
            ProjectionCount: 0,
            AuditLogCount: 0,
            SyncProvenanceCount: 0);
        using var factory = CreateFactoryWithMediator(new PurgeMediator(summary));
        using var client = factory.CreateClient();
        using var request = CreatePurgeRequest(definitionId, authenticated: true);

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var result = root.GetProperty("id");

        await Assert.That(root.GetProperty("success").GetBoolean()).IsFalse();
        await Assert.That(result.GetProperty("purged").GetBoolean()).IsFalse();
        await Assert.That(result.GetProperty("definitionId").GetGuid()).IsEqualTo(definitionId);
        await Assert.That(result.GetProperty("tenantId").GetGuid()).IsEqualTo(tenantId);
        await Assert.That(result.GetProperty("valueCount").GetInt32()).IsEqualTo(1);
        await Assert.That(result.GetProperty("optionCount").GetInt32()).IsEqualTo(2);
        await Assert.That(body).Contains("historical custom-property value");
    }

    private static WebApplicationFactory<Program> CreateFactoryWithMediator(IMediator mediator)
    {
        var factory = new AuthenticatedWebApplicationFactory();

        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IMediator>();
                services.AddSingleton(mediator);
            });
        });
    }

    private static HttpRequestMessage CreatePurgeRequest(Guid definitionId, bool authenticated)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/{definitionId}/purge")
        {
            Content = JsonContent.Create(new PurgeCustomPropertyDefinitionDto("operator cleanup"))
        };

        if (authenticated)
        {
            request.Headers.Add(
                TestAuthHandler.AuthHeaderName,
                TestAuthHandler.CreateAuthHeaderValue(
                    Guid.NewGuid(),
                    "Admin User",
                    (ClaimTypes.Role, "Admin")));
        }

        return request;
    }

    private sealed class PurgeMediator(CustomPropertyPurgeDependencySummary? summary = null) : IMediator
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
            => Task.CompletedTask;

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            object response = request switch
            {
                PurgeCustomPropertyDefinitionCommand command => CreatePurgeResponse(command),
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
                PurgeCustomPropertyDefinitionCommand command => Task.FromResult<object?>(CreatePurgeResponse(command)),
                _ => throw new InvalidOperationException($"Unexpected request type {request.GetType().Name}.")
            };

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        private BaseCommandResponse<CustomPropertyPurgeResultDto> CreatePurgeResponse(
            PurgeCustomPropertyDefinitionCommand command)
        {
            var dependencySummary = summary ?? new CustomPropertyPurgeDependencySummary(
                command.Id,
                Guid.NewGuid(),
                "custom_property_definition",
                OptionCount: 0,
                ValueCount: 0,
                ProjectionCount: 0,
                AuditLogCount: 0,
                SyncProvenanceCount: 0);
            var result = new CustomPropertyPurgeResultDto(
                dependencySummary.DefinitionId,
                dependencySummary.TenantId,
                dependencySummary.Scope,
                false,
                null,
                command.Reason,
                dependencySummary.OptionCount,
                dependencySummary.ValueCount,
                dependencySummary.ProjectionCount,
                dependencySummary.AuditLogCount,
                dependencySummary.SyncProvenanceCount);

            return new BaseCommandResponse<CustomPropertyPurgeResultDto>
            {
                Success = false,
                Message = "Custom-property definition purge blocked by existing dependencies.",
                Id = result,
                Errors = ["Cannot purge while historical custom-property value rows still exist."]
            };
        }
    }
}
