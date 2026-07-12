// ABOUTME: Reflection tests for the official C# MCP SDK contract surface.
// ABOUTME: Ensures tools, resources, prompts, and exposed parameters stay LLM-descriptive.

using System.ComponentModel;
using System.Reflection;
using Event.Api.IntegrationTests.Fixtures;
using Explore.API.Mcp;
using Explore.Application.Features.AiAssistant.Tools;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;
using TUnit.Core;

namespace ApiIntegrationTests.Features;

public sealed class McpSdkContractTests
{
    [Test]
    public void McpSurfaceTypesUseOfficialSdkTypeAttributes()
    {
        typeof(AiToolRegistryMcpTools).GetCustomAttribute<McpServerToolTypeAttribute>()
            .Should().NotBeNull();
        typeof(AiAssistantMcpTools).GetCustomAttribute<McpServerToolTypeAttribute>()
            .Should().NotBeNull();
        typeof(EventManagementMcpTools).GetCustomAttribute<McpServerToolTypeAttribute>()
            .Should().NotBeNull();
        typeof(AiAssistantMcpResources).GetCustomAttribute<McpServerResourceTypeAttribute>()
            .Should().NotBeNull();
        typeof(EventManagementMcpResources).GetCustomAttribute<McpServerResourceTypeAttribute>()
            .Should().NotBeNull();
        typeof(AiAssistantMcpPrompts).GetCustomAttribute<McpServerPromptTypeAttribute>()
            .Should().NotBeNull();
    }

    [Test]
    public void McpToolsPromptsAndResourcesHaveDescriptionsForLlmDiscovery()
    {
        var methods = new[]
        {
            RequiredMethod(typeof(AiToolRegistryMcpTools), nameof(AiToolRegistryMcpTools.ListAiToolContracts)),
            RequiredMethod(typeof(AiAssistantMcpTools), nameof(AiAssistantMcpTools.ProposeAiToolActionAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.SearchPublicEventsAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetPublicEventAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetPublicEventProgramSummaryAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.ListPublicEventSessionsAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.ListMyEventsAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventCreationContextAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventPublishReadinessAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventProgramManagementContextAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventCustomPropertiesContextAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventRegistrationsContextAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventTeamContextAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventTemplateCatalogContextAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventTemplateSyncContextAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventSessionTemplateSyncContextAsync)),
            RequiredMethod(typeof(AiAssistantMcpResources), nameof(AiAssistantMcpResources.ListConversationsAsync)),
            RequiredMethod(typeof(AiAssistantMcpResources), nameof(AiAssistantMcpResources.GetConversationAsync)),
            RequiredMethod(typeof(EventManagementMcpResources), nameof(EventManagementMcpResources.GetEventManagementContextAsync)),
            RequiredMethod(typeof(AiAssistantMcpPrompts), nameof(AiAssistantMcpPrompts.CreateEventDraftWithConfirmation)),
            RequiredMethod(typeof(AiAssistantMcpPrompts), nameof(AiAssistantMcpPrompts.ManageEventWithConfirmation))
        };

        foreach (var method in methods)
        {
            method.GetCustomAttribute<DescriptionAttribute>()?.Description
                .Should().NotBeNullOrWhiteSpace(method.Name);
        }
    }

    [Test]
    public void McpSchemaParametersHaveDescriptionsExceptInjectedCancellation()
    {
        var exposedMethods = new[]
        {
            RequiredMethod(typeof(AiAssistantMcpTools), nameof(AiAssistantMcpTools.ProposeAiToolActionAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.SearchPublicEventsAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetPublicEventAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetPublicEventProgramSummaryAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.ListPublicEventSessionsAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.ListMyEventsAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventCreationContextAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventPublishReadinessAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventProgramManagementContextAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventCustomPropertiesContextAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventRegistrationsContextAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventTeamContextAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventTemplateCatalogContextAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventTemplateSyncContextAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventSessionTemplateSyncContextAsync)),
            RequiredMethod(typeof(AiAssistantMcpResources), nameof(AiAssistantMcpResources.GetConversationAsync)),
            RequiredMethod(typeof(EventManagementMcpResources), nameof(EventManagementMcpResources.GetEventManagementContextAsync))
        };

        foreach (var parameter in exposedMethods.SelectMany(method => method.GetParameters()))
        {
            if (parameter.ParameterType == typeof(CancellationToken))
            {
                continue;
            }

            parameter.GetCustomAttribute<DescriptionAttribute>()?.Description
                .Should().NotBeNullOrWhiteSpace($"{parameter.Member.Name}.{parameter.Name}");
        }
    }

    [Test]
    public void McpCallableMethodsDeclareExplicitAuthorizationPosture()
    {
        var anonymousSafeMethods = new HashSet<MethodInfo>
        {
            RequiredMethod(typeof(AiToolRegistryMcpTools), nameof(AiToolRegistryMcpTools.ListAiToolContracts)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.SearchPublicEventsAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetPublicEventAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetPublicEventProgramSummaryAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.ListPublicEventSessionsAsync))
        };

        foreach (var method in McpCallableMethods())
        {
            if (anonymousSafeMethods.Contains(method))
            {
                method.GetCustomAttribute<AllowAnonymousAttribute>()
                    .Should().NotBeNull(method.Name);
                method.GetCustomAttribute<AuthorizeAttribute>()
                    .Should().BeNull(method.Name);
                continue;
            }

            method.GetCustomAttribute<AuthorizeAttribute>()
                .Should().NotBeNull(method.Name);
            method.GetCustomAttribute<AllowAnonymousAttribute>()
                .Should().BeNull(method.Name);
        }
    }

    [Test]
    public void McpAuthorizedMethodsUseApiKeyScopeAwarePolicies()
    {
        RequiredMethod(typeof(AiAssistantMcpTools), nameof(AiAssistantMcpTools.ProposeAiToolActionAsync))
            .GetCustomAttribute<AuthorizeAttribute>()?.Policy
            .Should().Be(McpAuthorizationPolicies.Propose);

        RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.ListMyEventsAsync))
            .GetCustomAttribute<AuthorizeAttribute>()?.Policy
            .Should().Be(McpAuthorizationPolicies.EventManagementRead);

        RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventCreationContextAsync))
            .GetCustomAttribute<AuthorizeAttribute>()?.Policy
            .Should().Be(McpAuthorizationPolicies.EventManagementRead);

        RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventPublishReadinessAsync))
            .GetCustomAttribute<AuthorizeAttribute>()?.Policy
            .Should().Be(McpAuthorizationPolicies.EventManagementRead);

        RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventProgramManagementContextAsync))
            .GetCustomAttribute<AuthorizeAttribute>()?.Policy
            .Should().Be(McpAuthorizationPolicies.EventManagementRead);

        RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventCustomPropertiesContextAsync))
            .GetCustomAttribute<AuthorizeAttribute>()?.Policy
            .Should().Be(McpAuthorizationPolicies.EventManagementRead);

        RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventRegistrationsContextAsync))
            .GetCustomAttribute<AuthorizeAttribute>()?.Policy
            .Should().Be(McpAuthorizationPolicies.EventManagementRead);

        RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventTeamContextAsync))
            .GetCustomAttribute<AuthorizeAttribute>()?.Policy
            .Should().Be(McpAuthorizationPolicies.EventManagementRead);

        RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventTemplateCatalogContextAsync))
            .GetCustomAttribute<AuthorizeAttribute>()?.Policy
            .Should().Be(McpAuthorizationPolicies.EventManagementRead);

        RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventTemplateSyncContextAsync))
            .GetCustomAttribute<AuthorizeAttribute>()?.Policy
            .Should().Be(McpAuthorizationPolicies.EventManagementRead);

        RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventSessionTemplateSyncContextAsync))
            .GetCustomAttribute<AuthorizeAttribute>()?.Policy
            .Should().Be(McpAuthorizationPolicies.EventManagementRead);

        RequiredMethod(typeof(AiAssistantMcpResources), nameof(AiAssistantMcpResources.ListConversationsAsync))
            .GetCustomAttribute<AuthorizeAttribute>()?.Policy
            .Should().Be(McpAuthorizationPolicies.Read);

        RequiredMethod(typeof(AiAssistantMcpResources), nameof(AiAssistantMcpResources.GetConversationAsync))
            .GetCustomAttribute<AuthorizeAttribute>()?.Policy
            .Should().Be(McpAuthorizationPolicies.Read);

        RequiredMethod(typeof(EventManagementMcpResources), nameof(EventManagementMcpResources.GetEventManagementContextAsync))
            .GetCustomAttribute<AuthorizeAttribute>()?.Policy
            .Should().Be(McpAuthorizationPolicies.EventManagementRead);

        RequiredMethod(typeof(AiAssistantMcpPrompts), nameof(AiAssistantMcpPrompts.CreateEventDraftWithConfirmation))
            .GetCustomAttribute<AuthorizeAttribute>()?.Policy
            .Should().Be(McpAuthorizationPolicies.Propose);

        RequiredMethod(typeof(AiAssistantMcpPrompts), nameof(AiAssistantMcpPrompts.ManageEventWithConfirmation))
            .GetCustomAttribute<AuthorizeAttribute>()?.Policy
            .Should().Be(McpAuthorizationPolicies.Propose);

        foreach (var definition in AiToolContractRegistry.CreateDefault().Definitions.Where(definition => definition.ExposeToMcp))
        {
            var projectedTool = new AiMcpProjectedProposalTool(definition);
            projectedTool.Metadata.OfType<AuthorizeAttribute>().Single().Policy
                .Should().Be(McpAuthorizationPolicies.Propose);
        }
    }

    [Test]
    public void McpSurfaceTypesDoNotPermitAnonymousAccess()
    {
        var surfaceTypes = new[]
        {
            typeof(AiToolRegistryMcpTools),
            typeof(AiAssistantMcpTools),
            typeof(EventManagementMcpTools),
            typeof(AiAssistantMcpResources),
            typeof(EventManagementMcpResources),
            typeof(AiAssistantMcpPrompts)
        };

        foreach (var type in surfaceTypes)
        {
            type.GetCustomAttribute<AllowAnonymousAttribute>()
                .Should().BeNull(type.FullName);
        }
    }

    [Test]
    public async Task ApiHostBindsMcpHttpTransportToStatelessStreamableHttpAtStartup()
    {
        await using var factory = CreateMcpEnabledFactory();
        using var client = factory.CreateClient();

        var transportOptions = factory.Services
            .GetRequiredService<IOptions<HttpServerTransportOptions>>()
            .Value;

        transportOptions.Stateless.Should().BeTrue();
#pragma warning disable MCP9004
        transportOptions.EnableLegacySse.Should().BeFalse();
#pragma warning restore MCP9004
    }

    [Test]
    public void ApiHostUsesStreamableHttpOnlyForProductMcpTransport()
    {
        var program = ReadRepoFile("src/Explore.API/Program.cs");

        program.Should().Contain(".WithHttpTransport(options =>");
        program.Should().Contain("options.Stateless = mcpAdapterSettings.Stateless");
        program.Should().Contain("builder.Services.PostConfigure<McpAdapterSettings>");
        program.Should().Contain("settings.EndpointPath = endpointPath.StartsWith('/')");
        program.Should().Contain("app.MapMcp(effectiveMcpAdapterSettings.EndpointPath)");
        program.Should().Contain(".AllowAnonymous()");
        program.Should().NotContain(".WithStdioServerTransport");
        program.Should().NotContain("options.EnableLegacySse");
    }

    [Test]
    public void ApiHostRegistersMcpSurfacesExplicitlyForAotReviewability()
    {
        var program = ReadRepoFile("src/Explore.API/Program.cs");

        program.Should().Contain(".WithTools<AiToolRegistryMcpTools>()");
        program.Should().Contain(".WithTools<AiAssistantMcpTools>()");
        program.Should().Contain(".WithTools<EventManagementMcpTools>()");
        program.Should().Contain(".WithResources<AiAssistantMcpResources>()");
        program.Should().Contain(".WithResources<EventManagementMcpResources>()");
        program.Should().Contain(".WithPrompts<AiAssistantMcpPrompts>()");
        program.Should().Contain("AiMcpProjectedToolOptionsSetup");
        program.Should().NotContain(".WithToolsFromAssembly");
        program.Should().NotContain(".WithResourcesFromAssembly");
        program.Should().NotContain(".WithPromptsFromAssembly");
    }

    [Test]
    public void ServiceDefaultsExportsBoundedMcpTelemetrySourceAndMeter()
    {
        var serviceDefaults = ReadRepoFile("src/Explore.ServiceDefaults/Extensions.cs");

        serviceDefaults.Should().Contain(".AddMeter(\"Explore.Mcp\")");
        serviceDefaults.Should().Contain(".AddSource(\"Explore.Mcp\")");
    }

    private static MethodInfo[] McpCallableMethods()
        =>
        [
            RequiredMethod(typeof(AiToolRegistryMcpTools), nameof(AiToolRegistryMcpTools.ListAiToolContracts)),
            RequiredMethod(typeof(AiAssistantMcpTools), nameof(AiAssistantMcpTools.ProposeAiToolActionAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.SearchPublicEventsAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetPublicEventAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetPublicEventProgramSummaryAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.ListPublicEventSessionsAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.ListMyEventsAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventCreationContextAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventPublishReadinessAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventProgramManagementContextAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventCustomPropertiesContextAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventRegistrationsContextAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventTeamContextAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventTemplateCatalogContextAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventTemplateSyncContextAsync)),
            RequiredMethod(typeof(EventManagementMcpTools), nameof(EventManagementMcpTools.GetEventSessionTemplateSyncContextAsync)),
            RequiredMethod(typeof(AiAssistantMcpResources), nameof(AiAssistantMcpResources.ListConversationsAsync)),
            RequiredMethod(typeof(AiAssistantMcpResources), nameof(AiAssistantMcpResources.GetConversationAsync)),
            RequiredMethod(typeof(EventManagementMcpResources), nameof(EventManagementMcpResources.GetEventManagementContextAsync)),
            RequiredMethod(typeof(AiAssistantMcpPrompts), nameof(AiAssistantMcpPrompts.CreateEventDraftWithConfirmation)),
            RequiredMethod(typeof(AiAssistantMcpPrompts), nameof(AiAssistantMcpPrompts.ManageEventWithConfirmation))
        ];

    private static MethodInfo RequiredMethod(Type type, string methodName)
        => type.GetMethod(methodName)
            ?? throw new InvalidOperationException($"{type.FullName}.{methodName} was not found.");

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

    private static string ReadRepoFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidatePath = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidatePath))
            {
                return File.ReadAllText(candidatePath);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find repository file '{relativePath}'.", relativePath);
    }
}
