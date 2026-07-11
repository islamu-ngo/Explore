// ABOUTME: MCP protocol tests for authenticated event-management read tools.
// ABOUTME: Verifies private management reads require auth and use existing my-events ownership filtering.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Event.Api.IntegrationTests.Builders;
using Event.Api.IntegrationTests.Fixtures;
using Event.Api.IntegrationTests.Seeds;
using Explore.Application.Hateoas;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Domain.Services.Scheduling;
using Explore.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core;

namespace ApiIntegrationTests.Features;

public sealed class EventManagementMcpAuthenticatedReadTests
{
    private static readonly string[] ManagementActionRelations =
    [
        LinkRelations.Edit,
        LinkRelations.Delete,
        LinkRelations.Publish,
        LinkRelations.PublishReadiness,
        LinkRelations.AddSession,
        LinkRelations.SessionCreateContext,
        LinkRelations.ModerateLight,
        LinkRelations.ModerateHeavy,
        LinkRelations.Unmoderate
    ];

    [Test]
    public async Task ListMyEvents_MatchesRestOwnershipAndCapsPageSize()
    {
        await using var factory = CreateMcpEnabledFactory();
        using var client = factory.CreateClient();
        var userId = Guid.CreateVersion7();
        var marker = Guid.NewGuid().ToString("N");
        var ownedTitle = $"MCP Owned Draft {marker}";
        var otherTitle = $"MCP Other Draft {marker}";

        await SeedOwnedEventsAsync(factory, userId, ownedTitle, otherTitle);

        using var restRequest = CreateAuthenticatedRequest(
            HttpMethod.Get,
            "/api/event/my?pageNumber=1&pageSize=50",
            userId);
        using var restResponse = await client.SendAsync(restRequest);
        var restText = await restResponse.Content.ReadAsStringAsync();
        var mcp = McpProtocolClient.Authenticated(client, userId);

        using var mcpResponse = await mcp.CallToolAsync("list_my_events", new JsonObject
        {
            ["pageNumber"] = 1,
            ["pageSize"] = 999
        });
        var mcpText = GetFirstTextContent(GetResult(mcpResponse));
        using var descriptor = JsonDocument.Parse(mcpText);

        restResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        restText.Should().Contain(ownedTitle);
        restText.Should().NotContain(otherTitle);

        mcpText.Should().Contain(ownedTitle);
        mcpText.Should().NotContain(otherTitle);
        descriptor.RootElement.GetProperty("PageSize").GetInt32().Should().Be(25);
        descriptor.RootElement.GetProperty("PageSizeWasClamped").GetBoolean().Should().BeTrue();
        descriptor.RootElement.GetProperty("TotalCount").GetInt32().Should().Be(1);
    }

    [Test]
    public async Task GetEventCreationContext_MatchesRestPolicyAndOmitsInternalRoleData()
    {
        await using var factory = CreateMcpEnabledFactory();
        using var client = factory.CreateClient();
        var seed = await SeedOrganizationPublisherAsync(factory);

        using var restRequest = CreateAuthenticatedRequest(
            HttpMethod.Get,
            "/api/event/creation-context",
            seed.UserId);
        using var restResponse = await client.SendAsync(restRequest);
        var restText = await restResponse.Content.ReadAsStringAsync();
        var mcp = McpProtocolClient.Authenticated(client, seed.UserId);

        using var mcpResponse = await mcp.CallToolAsync("get_event_creation_context", new JsonObject());
        var mcpText = GetFirstTextContent(GetResult(mcpResponse));
        using var descriptor = JsonDocument.Parse(mcpText);

        restResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        restText.Should().Contain("AI Test Publisher Organization");
        restText.Should().Contain(seed.OrganizationId.ToString());

        descriptor.RootElement.GetProperty("CanCreate").GetBoolean().Should().BeTrue();
        descriptor.RootElement.GetProperty("AllowPersonalPublishing").GetBoolean().Should().BeTrue();
        descriptor.RootElement.GetProperty("AllowOrganizationPublishing").GetBoolean().Should().BeTrue();
        descriptor.RootElement.GetProperty("DefaultPublisherMode").GetString().Should().Be("personal");
        descriptor.RootElement.GetProperty("PublisherOptionCount").GetInt32().Should().BeGreaterThanOrEqualTo(2);
        descriptor.RootElement.GetProperty("PublisherOptionsWereTruncated").GetBoolean().Should().BeFalse();

        var publisherOptions = descriptor.RootElement.GetProperty("PublisherOptions");
        var organizationOption = publisherOptions.EnumerateArray().Single(option =>
            option.GetProperty("PublisherId").ValueKind == JsonValueKind.String &&
            option.GetProperty("PublisherId").GetGuid() == seed.OrganizationId);
        organizationOption.GetProperty("PublisherMode").GetString().Should().Be("organization");
        organizationOption.GetProperty("DisplayName").GetString().Should().Be("AI Test Publisher Organization");
        organizationOption.GetProperty("CanPublish").GetBoolean().Should().BeTrue();

        mcpText.Should().NotContain("RoleId");
        mcpText.Should().NotContain("TenantId");
        mcpText.Should().NotContain("UserId");
    }

    [Test]
    public async Task EventManagementContext_DerivesActionAvailabilityFromRestHalLinks()
    {
        await using var factory = CreateMcpEnabledFactory();
        using var client = factory.CreateClient();
        var userId = Guid.CreateVersion7();
        var title = $"MCP Managed Draft {Guid.NewGuid():N}";
        var seed = await SeedOwnedDraftEventAsync(factory, userId, title);

        using var restRequest = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/event/{seed.EventId}", userId);
        using var restResponse = await client.SendAsync(restRequest);
        using var restDocument = await JsonDocument.ParseAsync(await restResponse.Content.ReadAsStreamAsync());
        var restLinks = ReadRestLinks(restDocument.RootElement);
        var mcp = McpProtocolClient.Authenticated(client, userId);

        using var resourceResponse = await mcp.ReadResourceAsync(
            $"islamu-event://events/{seed.EventId}/management-context");
        var resourceText = GetFirstResourceText(GetResult(resourceResponse));
        using var descriptor = JsonDocument.Parse(resourceText);

        restResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        restLinks.Keys.Should().Contain(LinkRelations.Edit);

        descriptor.RootElement.GetProperty("Found").GetBoolean().Should().BeTrue();
        descriptor.RootElement.GetProperty("EventId").GetGuid().Should().Be(seed.EventId);

        var context = descriptor.RootElement.GetProperty("Context");
        context.GetProperty("EventId").GetGuid().Should().Be(seed.EventId);
        context.GetProperty("ConcurrencyStamp").GetGuid().Should().Be(seed.ConcurrencyStamp);
        context.GetProperty("Title").GetString().Should().Be(title);
        context.GetProperty("PublishReadinessAvailable").GetBoolean()
            .Should().Be(restLinks.ContainsKey(LinkRelations.PublishReadiness));
        context.GetProperty("PublishReadiness").ValueKind.Should().NotBe(JsonValueKind.Null);
        context.GetProperty("PublishReadiness").GetProperty("EventId").GetGuid().Should().Be(seed.EventId);

        var actions = context.GetProperty("Actions")
            .EnumerateArray()
            .ToDictionary(action => action.GetProperty("Rel").GetString()!, StringComparer.Ordinal);

        foreach (var relation in ManagementActionRelations)
        {
            actions.Should().ContainKey(relation);
            var action = actions[relation];
            var isAvailableInRest = restLinks.TryGetValue(relation, out var restLink);
            action.GetProperty("Available").GetBoolean().Should().Be(isAvailableInRest, relation);

            if (isAvailableInRest)
            {
                action.GetProperty("Href").GetString().Should().Be(restLink.Href, relation);
                action.GetProperty("Method").GetString().Should().Be(restLink.Method, relation);
                continue;
            }

            action.GetProperty("Href").ValueKind.Should().Be(JsonValueKind.Null, relation);
            action.GetProperty("Method").ValueKind.Should().Be(JsonValueKind.Null, relation);
        }

        resourceText.Should().NotContain("TenantId");
        resourceText.Should().NotContain("UserId");
        resourceText.Should().NotContain("RoleId");
    }

    [Test]
    public async Task GetEventPublishReadiness_MatchesRestReadinessWhenHalAffordanceIsAvailable()
    {
        await using var factory = CreateMcpEnabledFactory();
        using var client = factory.CreateClient();
        var userId = Guid.CreateVersion7();
        var title = $"MCP Readiness Draft {Guid.NewGuid():N}";
        var seed = await SeedOwnedDraftEventAsync(factory, userId, title);

        using var restDetailRequest = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/event/{seed.EventId}", userId);
        using var restDetailResponse = await client.SendAsync(restDetailRequest);
        using var restDetailDocument = await JsonDocument.ParseAsync(await restDetailResponse.Content.ReadAsStreamAsync());
        var restLinks = ReadRestLinks(restDetailDocument.RootElement);

        using var restReadinessRequest = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/event/{seed.EventId}/publish-readiness", userId);
        using var restReadinessResponse = await client.SendAsync(restReadinessRequest);
        using var restReadiness = await JsonDocument.ParseAsync(await restReadinessResponse.Content.ReadAsStreamAsync());
        var mcp = McpProtocolClient.Authenticated(client, userId);

        using var mcpResponse = await mcp.CallToolAsync("get_event_publish_readiness", new JsonObject
        {
            ["eventId"] = seed.EventId.ToString()
        });
        var mcpText = GetFirstTextContent(GetResult(mcpResponse));
        using var descriptor = JsonDocument.Parse(mcpText);

        restDetailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        restLinks.Should().ContainKey(LinkRelations.PublishReadiness);
        restReadinessResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var restErrors = restReadiness.RootElement.GetProperty("errors");
        var readiness = descriptor.RootElement.GetProperty("PublishReadiness");
        descriptor.RootElement.GetProperty("Found").GetBoolean().Should().BeTrue();
        descriptor.RootElement.GetProperty("Available").GetBoolean().Should().BeTrue();
        descriptor.RootElement.GetProperty("FailureCode").ValueKind.Should().Be(JsonValueKind.Null);
        readiness.GetProperty("EventId").GetGuid().Should().Be(seed.EventId);
        readiness.GetProperty("IsReady").GetBoolean()
            .Should().Be(restReadiness.RootElement.GetProperty("isReady").GetBoolean());
        readiness.GetProperty("ErrorCount").GetInt32().Should().Be(restErrors.GetArrayLength());
        readiness.GetProperty("Errors").GetArrayLength().Should().BeLessThanOrEqualTo(25);

        if (restErrors.GetArrayLength() > 0)
        {
            var restCodes = restErrors
                .EnumerateArray()
                .Select(error => error.GetProperty("code").GetString())
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .ToArray();
            var mcpCodes = readiness.GetProperty("Errors")
                .EnumerateArray()
                .Select(error => error.GetProperty("Code").GetString())
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .ToArray();

            mcpCodes.Should().BeSubsetOf(restCodes);
        }

        mcpText.Should().NotContain("TenantId");
        mcpText.Should().NotContain("UserId");
        mcpText.Should().NotContain("RoleId");
    }

    [Test]
    public async Task GetEventPublishReadiness_ReturnsUnavailableWhenHalAffordanceIsAbsent()
    {
        await using var factory = CreateMcpEnabledFactory();
        using var client = factory.CreateClient();
        var userId = Guid.CreateVersion7();
        var title = $"MCP Readiness Published {Guid.NewGuid():N}";
        var seed = await SeedOwnedDraftEventAsync(factory, userId, title);
        await SetEventStatusAsync(factory, seed.EventId, EventStatusEnum.Published);

        using var restDetailRequest = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/event/{seed.EventId}", userId);
        using var restDetailResponse = await client.SendAsync(restDetailRequest);
        using var restDetailDocument = await JsonDocument.ParseAsync(await restDetailResponse.Content.ReadAsStreamAsync());
        var restLinks = ReadRestLinks(restDetailDocument.RootElement);
        var mcp = McpProtocolClient.Authenticated(client, userId);

        using var mcpResponse = await mcp.CallToolAsync("get_event_publish_readiness", new JsonObject
        {
            ["eventId"] = seed.EventId.ToString()
        });
        using var descriptor = JsonDocument.Parse(GetFirstTextContent(GetResult(mcpResponse)));

        restDetailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        restLinks.Should().NotContainKey(LinkRelations.PublishReadiness);
        descriptor.RootElement.GetProperty("Found").GetBoolean().Should().BeTrue();
        descriptor.RootElement.GetProperty("Available").GetBoolean().Should().BeFalse();
        descriptor.RootElement.GetProperty("FailureCode").GetString().Should().Be("not_available");
        descriptor.RootElement.GetProperty("PublishReadiness").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Test]
    public async Task AuthenticatedSubResourceReadTools_ReturnBoundedHalGatedContexts()
    {
        await using var factory = CreateMcpEnabledFactory();
        using var client = factory.CreateClient();
        var userId = Guid.CreateVersion7();
        var title = $"MCP Subresource Draft {Guid.NewGuid():N}";
        var seed = await SeedOwnedDraftEventAsync(factory, userId, title);
        var sessionId = await SeedManagedSubResourcesAsync(factory, seed.EventId);
        var mcp = McpProtocolClient.Authenticated(client, userId);

        using var programResponse = await mcp.CallToolAsync("get_event_program_management_context", new JsonObject
        {
            ["eventId"] = seed.EventId.ToString()
        });
        var programText = GetFirstTextContent(GetResult(programResponse));
        using var program = JsonDocument.Parse(programText);
        var programContext = program.RootElement.GetProperty("Context");

        program.RootElement.GetProperty("Found").GetBoolean().Should().BeTrue();
        program.RootElement.GetProperty("Available").GetBoolean().Should().BeTrue();
        programContext.GetProperty("EventId").GetGuid().Should().Be(seed.EventId);
        programContext.GetProperty("SessionCount").GetInt32().Should().Be(1);
        programContext.GetProperty("SessionGroupCount").GetInt32().Should().Be(1);
        programContext.GetProperty("DayCount").GetInt32().Should().Be(1);
        programContext.GetProperty("AgendaItemCount").GetInt32().Should().Be(1);
        programText.Should().Contain("MCP Management Session");
        programText.Should().Contain("MCP Management Track");
        programText.Should().NotContain("TenantId");
        programText.Should().NotContain("UserId");

        using var customPropertiesResponse = await mcp.CallToolAsync("get_event_custom_properties_context", new JsonObject
        {
            ["eventId"] = seed.EventId.ToString(),
            ["pageSize"] = 999
        });
        var customPropertiesText = GetFirstTextContent(GetResult(customPropertiesResponse));
        using var customProperties = JsonDocument.Parse(customPropertiesText);
        var customPropertiesContext = customProperties.RootElement.GetProperty("Context");

        customProperties.RootElement.GetProperty("Found").GetBoolean().Should().BeTrue();
        customProperties.RootElement.GetProperty("Available").GetBoolean().Should().BeTrue();
        customPropertiesContext.GetProperty("PageSize").GetInt32().Should().Be(25);
        customPropertiesContext.GetProperty("PageSizeWasClamped").GetBoolean().Should().BeTrue();
        customPropertiesContext.GetProperty("TotalDefinitionCount").GetInt32().Should().Be(1);
        customPropertiesContext.GetProperty("ValueCount").GetInt32().Should().Be(1);
        customPropertiesText.Should().Contain("MCP Track Notes");
        customPropertiesText.Should().Contain("Bring notebook");
        customPropertiesText.Should().NotContain("TenantId");
        customPropertiesText.Should().NotContain("UserId");

        using var registrationsResponse = await mcp.CallToolAsync("get_event_registrations_context", new JsonObject
        {
            ["eventId"] = seed.EventId.ToString(),
            ["pageSize"] = 999
        });
        var registrationsText = GetFirstTextContent(GetResult(registrationsResponse));
        using var registrations = JsonDocument.Parse(registrationsText);
        var registrationsContext = registrations.RootElement.GetProperty("Context");

        registrations.RootElement.GetProperty("Found").GetBoolean().Should().BeTrue();
        registrations.RootElement.GetProperty("Available").GetBoolean().Should().BeTrue();
        registrationsContext.GetProperty("PageSize").GetInt32().Should().Be(100);
        registrationsContext.GetProperty("PageSizeWasClamped").GetBoolean().Should().BeTrue();
        registrationsContext.GetProperty("TotalRegistrationCount").GetInt32().Should().Be(1);
        registrationsText.Should().Contain("MCP Management Session");
        registrationsText.Should().Contain("Approved");
        registrationsText.Should().NotContain("MCP Management Attendee");
        registrationsText.Should().NotContain("TenantId");
        registrationsText.Should().NotContain("UserId");
        registrationsText.Should().NotContain("UserFullName");
        registrationsText.Should().NotContain("UserEmail");

        using var teamResponse = await mcp.CallToolAsync("get_event_team_context", new JsonObject
        {
            ["eventId"] = seed.EventId.ToString()
        });
        var teamText = GetFirstTextContent(GetResult(teamResponse));
        using var team = JsonDocument.Parse(teamText);

        team.RootElement.GetProperty("Found").GetBoolean().Should().BeTrue();
        team.RootElement.GetProperty("Available").GetBoolean().Should().BeTrue();
        team.RootElement.GetProperty("Context").GetProperty("CurrentUserPermissions")
            .GetProperty("EventId").GetGuid().Should().Be(seed.EventId);
        teamText.Should().NotContain("TenantId");
        teamText.Should().NotContain("UserId");

        using var templateCatalogResponse = await mcp.CallToolAsync("get_event_template_catalog_context", new JsonObject
        {
            ["eventId"] = seed.EventId.ToString(),
            ["pageSize"] = 999
        });
        var templateCatalogText = GetFirstTextContent(GetResult(templateCatalogResponse));
        using var templateCatalog = JsonDocument.Parse(templateCatalogText);

        templateCatalog.RootElement.GetProperty("Found").GetBoolean().Should().BeTrue();
        templateCatalog.RootElement.GetProperty("Available").GetBoolean().Should().BeTrue();
        templateCatalog.RootElement.GetProperty("Context").GetProperty("PageSize").GetInt32().Should().Be(25);
        templateCatalogText.Should().NotContain("TenantId");
        templateCatalogText.Should().NotContain("UserId");

        using var templateSyncResponse = await mcp.CallToolAsync("get_event_template_sync_context", new JsonObject
        {
            ["eventId"] = seed.EventId.ToString()
        });
        var templateSyncText = GetFirstTextContent(GetResult(templateSyncResponse));
        using var templateSync = JsonDocument.Parse(templateSyncText);

        templateSync.RootElement.GetProperty("Found").GetBoolean().Should().BeTrue();
        templateSync.RootElement.GetProperty("Available").GetBoolean().Should().BeTrue();
        templateSync.RootElement.GetProperty("Context").GetProperty("DiffAvailable").GetBoolean().Should().BeFalse();
        templateSync.RootElement.GetProperty("Context").GetProperty("DiffFailureCode").GetString().Should().Be("not_requested");
        templateSyncText.Should().NotContain("TenantId");
        templateSyncText.Should().NotContain("UserId");

        using var sessionTemplateSyncResponse = await mcp.CallToolAsync("get_event_session_template_sync_context", new JsonObject
        {
            ["eventId"] = seed.EventId.ToString(),
            ["sessionId"] = sessionId.ToString()
        });
        var sessionTemplateSyncText = GetFirstTextContent(GetResult(sessionTemplateSyncResponse));
        using var sessionTemplateSync = JsonDocument.Parse(sessionTemplateSyncText);

        sessionTemplateSync.RootElement.GetProperty("Found").GetBoolean().Should().BeTrue();
        sessionTemplateSync.RootElement.GetProperty("Available").GetBoolean().Should().BeTrue();
        sessionTemplateSync.RootElement.GetProperty("Context").GetProperty("SessionId").GetGuid().Should().Be(sessionId);
        sessionTemplateSyncText.Should().NotContain("TenantId");
        sessionTemplateSyncText.Should().NotContain("UserId");
    }

    private static AuthenticatedWebApplicationFactory CreateMcpEnabledFactory()
    {
        var factory = new AuthenticatedWebApplicationFactory
        {
            AuthorizationProviderOverride = new StubAuthorizationProvider()
        };
        factory.AdditionalConfiguration["Mcp:Enabled"] = "true";
        factory.AdditionalConfiguration["Mcp:EndpointPath"] = "/mcp";
        factory.AdditionalConfiguration["Mcp:Stateless"] = "true";
        factory.AdditionalConfiguration["Mcp:EnableLegacySse"] = "false";
        return factory;
    }

    private static HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url, Guid userId)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add(TestAuthHandler.AuthHeaderName, TestAuthHandler.CreateAuthHeaderValue(userId));
        return request;
    }

    private static async Task SeedOwnedEventsAsync(
        AuthenticatedWebApplicationFactory factory,
        Guid userId,
        string ownedTitle,
        string otherTitle)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var tenant = await context.Tenants.FindAsync(PlatformDefaults.DefaultTenantId);
        if (tenant is null)
        {
            tenant = new TenantBuilder()
                .WithId(PlatformDefaults.DefaultTenantId)
                .WithFullName("Default MCP Authenticated Event Tenant")
                .WithSlug("default-mcp-authenticated-event")
                .Build();
            context.Tenants.Add(tenant);
            await context.SaveChangesAsync();
        }

        var owner = new UserBuilder()
            .WithId(userId)
            .Build();
        var otherUser = new UserBuilder().Build();
        context.Users.AddRange(owner, otherUser);
        await context.SaveChangesAsync();

        var ownerActor = new ActorBuilder()
            .WithTenantId(tenant.Id)
            .WithUserId(owner.Id)
            .WithDisplayName("MCP Authenticated Owner")
            .Build();
        var otherActor = new ActorBuilder()
            .WithTenantId(tenant.Id)
            .WithUserId(otherUser.Id)
            .WithDisplayName("MCP Authenticated Other")
            .Build();
        context.Actors.AddRange(ownerActor, otherActor);
        await context.SaveChangesAsync();

        owner.ActorId = ownerActor.Id;
        owner.DefaultActorId = ownerActor.Id;
        otherUser.ActorId = otherActor.Id;
        otherUser.DefaultActorId = otherActor.Id;

        context.Events.AddRange(
            new EventBuilder()
                .WithTitle(ownedTitle)
                .WithDescription($"Description for {ownedTitle}")
                .WithActorId(ownerActor.Id)
                .WithTenantId(tenant.Id)
                .WithStatus(EventStatusEnum.Draft)
                .WithVisibility(VisibilityTypeEnum.Private)
                .WithSessionDates(
                    DateOnly.FromDateTime(DateTime.UtcNow.AddDays(4)),
                    DateOnly.FromDateTime(DateTime.UtcNow.AddDays(4)))
                .Build(),
            new EventBuilder()
                .WithTitle(otherTitle)
                .WithDescription($"Description for {otherTitle}")
                .WithActorId(otherActor.Id)
                .WithTenantId(tenant.Id)
                .WithStatus(EventStatusEnum.Draft)
                .WithVisibility(VisibilityTypeEnum.Private)
                .WithSessionDates(
                    DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
                    DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)))
                .Build());
        await context.SaveChangesAsync();
    }

    private static async Task<OwnedDraftEventSeed> SeedOwnedDraftEventAsync(
        AuthenticatedWebApplicationFactory factory,
        Guid userId,
        string title)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var tenant = await context.Tenants.FindAsync(PlatformDefaults.DefaultTenantId);
        if (tenant is null)
        {
            tenant = new TenantBuilder()
                .WithId(PlatformDefaults.DefaultTenantId)
                .WithFullName("Default MCP Management Event Tenant")
                .WithSlug("default-mcp-management-event")
                .Build();
            context.Tenants.Add(tenant);
            await context.SaveChangesAsync();
        }

        var owner = new UserBuilder()
            .WithId(userId)
            .Build();
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var ownerActor = new ActorBuilder()
            .WithTenantId(tenant.Id)
            .WithUserId(owner.Id)
            .WithDisplayName("MCP Management Owner")
            .Build();
        context.Actors.Add(ownerActor);
        await context.SaveChangesAsync();

        owner.ActorId = ownerActor.Id;
        owner.DefaultActorId = ownerActor.Id;

        var @event = new EventBuilder()
            .WithTitle(title)
            .WithDescription($"Description for {title}")
            .WithActorId(ownerActor.Id)
            .WithTenantId(tenant.Id)
            .WithStatus(EventStatusEnum.Draft)
            .WithVisibility(VisibilityTypeEnum.Private)
            .WithSessionDates(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(6)),
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(6)))
            .Build();
        context.Events.Add(@event);
        await context.SaveChangesAsync();

        @event.CreatedBy = userId;
        await context.SaveChangesAsync();

        return new OwnedDraftEventSeed(@event.Id, @event.ConcurrencyStamp);
    }

    private static async Task SetEventStatusAsync(
        AuthenticatedWebApplicationFactory factory,
        Guid eventId,
        EventStatusEnum status)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var @event = await context.Events.FindAsync(eventId);
        @event.Should().NotBeNull();
        @event!.EventStatusId = (int)status;
        await context.SaveChangesAsync();
    }

    private static async Task<Guid> SeedManagedSubResourcesAsync(
        AuthenticatedWebApplicationFactory factory,
        Guid eventId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var @event = await context.Events.FindAsync(eventId);
        @event.Should().NotBeNull();
        @event!.Timezone = "UTC";
        var tenantId = @event.TenantId;
        var start = DateTimeOffset.UtcNow.AddDays(10);
        var calculator = new EventScheduleProjectionCalculator();

        var day = new EventDay
        {
            EventId = eventId,
            Event = null!,
            LocalDate = DateOnly.FromDateTime(start.UtcDateTime),
            Label = "MCP Management Day",
            SortOrder = 1,
            IsPublished = true,
            AllowsDayScopeRegistration = true,
            TenantId = tenantId,
            Tenant = null!,
            ConcurrencyStamp = Guid.NewGuid()
        };
        context.EventDays.Add(day);

        var sessionGroup = new EventSessionGroup
        {
            EventId = eventId,
            Event = null!,
            Name = "MCP Management Track",
            Slug = "mcp-management-track",
            Color = "#2255aa",
            SortOrder = 1,
            IsPublished = true,
            TenantId = tenantId,
            Tenant = null!,
            ConcurrencyStamp = Guid.NewGuid()
        };
        context.EventSessionGroups.Add(sessionGroup);

        var session = new EventSession
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Event = null!,
            TenantId = tenantId,
            Tenant = null!,
            Title = "MCP Management Session",
            SortOrder = 1,
            EventSessionKindId = (int)EventSessionKindEnum.Talk,
            RegistrationModeId = 1,
            MaxAudienceAttendees = 80,
            ConcurrencyStamp = Guid.NewGuid()
        };
        session.Reschedule(start, start.AddHours(1), "UTC", calculator);
        context.EventSessions.Add(session);

        var agendaItem = new EventAgendaItem
        {
            EventId = eventId,
            Event = null!,
            EventDay = day,
            Title = "MCP Management Break",
            SortOrder = 1,
            TenantId = tenantId,
            Tenant = null!,
            ConcurrencyStamp = Guid.NewGuid()
        };
        agendaItem.Reschedule(start.AddHours(1), start.AddHours(2), "UTC", calculator);
        context.EventAgendaItems.Add(agendaItem);

        var definition = new EventCustomPropertyDefinition
        {
            EventId = eventId,
            Event = null!,
            TenantId = tenantId,
            Tenant = null!,
            Namespace = "mcp.management",
            Key = "track_notes",
            DisplayName = "MCP Track Notes",
            PropertyType = PropertyType.Text,
            IsActive = true,
            ExposureLevel = ExposureLevel.OrganizerOnly,
            SortOrder = 1,
            InstantiatedAt = DateTimeOffset.UtcNow,
            ConcurrencyStamp = Guid.NewGuid()
        };
        context.EventCustomPropertyDefinitions.Add(definition);
        await context.SaveChangesAsync();

        context.EventSessionGroupSessions.Add(new EventSessionGroupSession
        {
            EventId = eventId,
            Event = null!,
            EventSessionGroupId = sessionGroup.Id,
            EventSessionGroup = null!,
            EventSessionId = session.Id,
            EventSession = null!,
            IsPrimary = true,
            SortOrder = 1,
            TenantId = tenantId,
            Tenant = null!
        });
        context.EventCustomPropertyValues.Add(new EventCustomPropertyValue
        {
            EventCustomPropertyDefinitionId = definition.Id,
            Definition = null,
            EventId = eventId,
            Event = null,
            TenantId = tenantId,
            Tenant = null,
            Ordinal = 0,
            TextValue = "Bring notebook",
            ConcurrencyStamp = Guid.NewGuid()
        });
        var attendee = new UserBuilder()
            .WithFirstName("MCP Management")
            .WithLastName("Attendee")
            .Build();
        context.Users.Add(attendee);
        context.EventRegistrations.Add(new EventRegistration
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Event = null!,
            EventSessionId = session.Id,
            EventSession = session,
            UserId = attendee.Id,
            User = attendee,
            ApprovalStatusId = (int)ApprovalStatusEnum.Approved,
            TenantId = tenantId,
            Tenant = null!
        });
        await context.SaveChangesAsync();

        return session.Id;
    }

    private static async Task<TenantScenarioSeed.TenantOrganizationScenarioResult> SeedOrganizationPublisherAsync(
        AuthenticatedWebApplicationFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        return await TenantScenarioSeed.SeedActiveTenantWithOrganizationPublisherAsync(context);
    }

    private static JsonElement GetResult(JsonDocument document)
        => document.RootElement.GetProperty("result");

    private static string GetFirstTextContent(JsonElement result)
    {
        var content = result.GetProperty("content");
        content.ValueKind.Should().Be(JsonValueKind.Array);
        content.GetArrayLength().Should().BeGreaterThan(0);
        return content[0].GetProperty("text").GetString() ?? string.Empty;
    }

    private static string GetFirstResourceText(JsonElement result)
    {
        var contents = result.GetProperty("contents");
        contents.ValueKind.Should().Be(JsonValueKind.Array);
        contents.GetArrayLength().Should().BeGreaterThan(0);
        return contents[0].GetProperty("text").GetString() ?? string.Empty;
    }

    private static Dictionary<string, RestHalLink> ReadRestLinks(JsonElement resource)
    {
        var links = resource.GetProperty("_links");
        return links.EnumerateObject().ToDictionary(
            property => property.Name,
            property => new RestHalLink(
                ReadNullableString(property.Value, "href"),
                ReadNullableString(property.Value, "method")),
            StringComparer.Ordinal);
    }

    private static string? ReadNullableString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private sealed record OwnedDraftEventSeed(Guid EventId, Guid ConcurrencyStamp);

    private sealed record RestHalLink(string? Href, string? Method);

    private sealed class McpProtocolClient(HttpClient client, Guid userId)
    {
        private int _nextId;

        public static McpProtocolClient Authenticated(HttpClient client, Guid userId)
            => new(client, userId);

        public Task<JsonDocument> CallToolAsync(string toolName, JsonObject arguments)
            => SendJsonRpcAsync("tools/call", new JsonObject
            {
                ["name"] = toolName,
                ["arguments"] = arguments
            });

        public Task<JsonDocument> ReadResourceAsync(string uri)
            => SendJsonRpcAsync("resources/read", new JsonObject
            {
                ["uri"] = uri
            });

        private async Task<JsonDocument> SendJsonRpcAsync(string method, JsonObject? parameters)
        {
            var requestBody = new JsonObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = Interlocked.Increment(ref _nextId),
                ["method"] = method
            };

            if (parameters is not null)
            {
                requestBody["params"] = parameters;
            }

            using var response = await SendRawAsync(requestBody.ToJsonString());
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var document = await ReadJsonRpcDocumentAsync(response);
            document.RootElement.TryGetProperty("error", out _).Should().BeFalse(document.RootElement.GetRawText());
            document.RootElement.TryGetProperty("result", out _).Should().BeTrue(document.RootElement.GetRawText());
            return document;
        }

        private async Task<HttpResponseMessage> SendRawAsync(string json)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/mcp");
            request.Headers.Add(TestAuthHandler.AuthHeaderName, TestAuthHandler.CreateAuthHeaderValue(userId));
            request.Headers.Add("ProtocolVersion", "2025-06-18");
            request.Headers.Add("MCP-Protocol-Version", "2025-06-18");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return await client.SendAsync(request);
        }

        private static async Task<JsonDocument> ReadJsonRpcDocumentAsync(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            var trimmed = body.TrimStart();
            if (trimmed.StartsWith('{'))
            {
                return JsonDocument.Parse(trimmed);
            }

            foreach (var line in body.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var payload = line[5..].Trim();
                if (payload.StartsWith('{'))
                {
                    return JsonDocument.Parse(payload);
                }
            }

            throw new InvalidOperationException("The MCP response did not contain a JSON-RPC message.");
        }
    }
}
