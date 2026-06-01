// ABOUTME: API contract tests for authenticated AI assistant conversation endpoints.
// ABOUTME: Verifies thin MediatR dispatch, idempotency headers, ProblemDetails, and HAL payload shape.

namespace Event.Api.IntegrationTests.Features;

using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Controllers;
using Explore.API.Extensions;
using Explore.API.Hateoas;
using Explore.Application.DTOs.Ai;
using Explore.Application.Features.AiAssistant.Requests.Commands;
using Explore.Application.Features.AiAssistant.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using Explore.Application.Serialization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using NSubstitute;

public sealed class AiAssistantControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IHateoasLinkGenerator _linkGenerator = Substitute.For<IHateoasLinkGenerator>();
    private readonly IResourceAssembler<AiConversationDto, AiConversationSummaryDto> _conversationAssembler =
        Substitute.For<IResourceAssembler<AiConversationDto, AiConversationSummaryDto>>();

    [Test]
    public async Task AiRoutes_AreAuthenticatedAndUseStableRouteNames()
    {
        await Assert.That(typeof(AiAssistantController).GetCustomAttribute<AuthorizeAttribute>()).IsNotNull();
        await Assert.That(typeof(AiAssistantController).GetCustomAttribute<EndpointClassificationAttribute>()?.Class)
            .IsEqualTo(EndpointClass.Authenticated);
        await Assert.That(typeof(AiAssistantController).GetCustomAttribute<ApiVersionAttribute>()?.Versions.Single().ToString())
            .IsEqualTo("0.1");

        AssertRoute(nameof(AiAssistantController.GetBootstrap), typeof(HttpGetAttribute), "bootstrap", RouteNames.GetAiAssistantBootstrap);
        AssertRoute(nameof(AiAssistantController.GetConversations), typeof(HttpGetAttribute), "conversations", RouteNames.GetAiConversations);
        AssertRoute(nameof(AiAssistantController.CreateConversation), typeof(HttpPostAttribute), "conversations", RouteNames.CreateAiConversation);
        AssertRoute(nameof(AiAssistantController.GetConversation), typeof(HttpGetAttribute), "conversations/{conversationId:guid}", RouteNames.GetAiConversation);
        AssertRoute(nameof(AiAssistantController.SendMessage), typeof(HttpPostAttribute), "conversations/{conversationId:guid}/messages", RouteNames.SendAiMessage);
        AssertRoute(nameof(AiAssistantController.GetRunStatus), typeof(HttpGetAttribute), "conversations/{conversationId:guid}/runs/{runId:guid}", RouteNames.GetAiRunStatus);
    }

    [Test]
    public async Task GetConversations_DispatchesQueryAndUsesConversationAssembler()
    {
        var conversation = CreateSummary(Guid.CreateVersion7(), "Active");
        var conversations = new[] { conversation };
        var expected = HalCollectionResource<AiConversationSummaryDto>.Create(
            [new HalResource<AiConversationSummaryDto>(conversation)],
            pageNumber: 1,
            pageSize: 20,
            totalCount: 1,
            links: new Dictionary<string, HalLink>());
        _mediator.Send(Arg.Any<GetAiConversationListQuery>(), Arg.Any<CancellationToken>())
            .Returns(conversations);
        _conversationAssembler.ToCollectionResource(
                Arg.Any<IEnumerable<AiConversationSummaryDto>>(),
                RouteNames.GetAiConversations,
                Arg.Any<object>(),
                Arg.Any<HttpContext>())
            .Returns(expected);
        var controller = CreateController();

        var actionResult = await controller.GetConversations(limit: 25, CancellationToken.None);

        var ok = actionResult.Result as OkObjectResult;
        await Assert.That(ok).IsNotNull();
        await Assert.That(ok!.Value).IsEqualTo(expected);
        await _mediator.Received(1).Send(
            Arg.Is<GetAiConversationListQuery>(query => query.Limit == 25),
            Arg.Any<CancellationToken>());
        await _conversationAssembler.Received(1).ToCollectionResource(
            Arg.Is<IEnumerable<AiConversationSummaryDto>>(items => items.Single() == conversation),
            RouteNames.GetAiConversations,
            Arg.Any<object>(),
            Arg.Any<HttpContext>());
    }

    [Test]
    public async Task SendMessage_PropagatesIdempotencyHeaderAndReturnsAcceptedRunLocation()
    {
        var conversationId = Guid.CreateVersion7();
        var runId = Guid.CreateVersion7();
        var dto = new SendAiMessageRequestDto
        {
            Content = "Plan this event.",
            IdempotencyKey = "body-key"
        };
        var response = Success(runId);
        _mediator.Send(Arg.Any<SendAiMessageCommand>(), Arg.Any<CancellationToken>())
            .Returns(response);
        var controller = CreateController();

        var actionResult = await controller.SendMessage(conversationId, dto, "header-key", CancellationToken.None);

        var accepted = actionResult.Result as AcceptedAtRouteResult;
        await Assert.That(accepted).IsNotNull();
        await Assert.That(accepted!.RouteName).IsEqualTo(RouteNames.GetAiRunStatus);
        await Assert.That(accepted.Value).IsEqualTo(response);
        await Assert.That(RouteValue<Guid>(accepted.RouteValues, "conversationId")).IsEqualTo(conversationId);
        await Assert.That(RouteValue<Guid>(accepted.RouteValues, "runId")).IsEqualTo(runId);
        await _mediator.Received(1).Send(
            Arg.Is<SendAiMessageCommand>(command =>
                command.ConversationId == conversationId &&
                command.Message == dto &&
                command.Message.IdempotencyKey == "header-key"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SendMessage_UsesAiAssistantRateLimitPolicy()
    {
        var method = typeof(AiAssistantController).GetMethod(nameof(AiAssistantController.SendMessage))!;
        var attribute = method.GetCustomAttribute<EnableRateLimitingAttribute>();

        await Assert.That(attribute).IsNotNull();
        await Assert.That(attribute!.PolicyName).IsEqualTo(RateLimitingExtensions.AiAssistantPolicy);
    }

    [Test]
    public async Task SendMessage_WhenProviderNotReady_ReturnsSafeProblemDetails()
    {
        var conversationId = Guid.CreateVersion7();
        _mediator.Send(Arg.Any<SendAiMessageCommand>(), Arg.Any<CancellationToken>())
            .Returns(Failure("AI provider is not ready.", "provider_not_ready"));
        var controller = CreateController();

        var actionResult = await controller.SendMessage(
            conversationId,
            new SendAiMessageRequestDto { Content = "hello", IdempotencyKey = "key" },
            null,
            CancellationToken.None);

        var result = actionResult.Result as ObjectResult;
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.StatusCode).IsEqualTo(StatusCodes.Status503ServiceUnavailable);
        await Assert.That(result.ContentTypes).Contains("application/problem+json");
        var problem = result.Value as ProblemDetails;
        await Assert.That(problem).IsNotNull();
        await Assert.That(problem!.Title).IsEqualTo("AI provider unavailable");
        await Assert.That(problem.Extensions["code"]).IsEqualTo("provider_not_ready");
        await Assert.That(problem.Detail).DoesNotContain("sk-");
        await Assert.That(problem.Detail).DoesNotContain("prompt");
    }

    [Test]
    public async Task GetRunStatus_ReturnsHalResourceWithSelfAndConversationLinks()
    {
        var conversationId = Guid.CreateVersion7();
        var runId = Guid.CreateVersion7();
        _mediator.Send(Arg.Any<GetAiRunStatusQuery>(), Arg.Any<CancellationToken>())
            .Returns(new AiRunDto
            {
                Id = runId,
                Status = "Succeeded",
                Provider = "fake",
                ModelId = "fake-ai-assistant-v1",
                QueuedAt = DateTime.UtcNow
            });
        _linkGenerator.GeneratePath(RouteNames.GetAiRunStatus, Arg.Any<object>(), Arg.Any<HttpContext>())
            .Returns($"/api/ai/assistant/conversations/{conversationId}/runs/{runId}");
        _linkGenerator.GeneratePath(RouteNames.GetAiConversation, Arg.Any<object>(), Arg.Any<HttpContext>())
            .Returns($"/api/ai/assistant/conversations/{conversationId}");
        var controller = CreateController();

        var actionResult = await controller.GetRunStatus(conversationId, runId, CancellationToken.None);

        var ok = actionResult.Result as OkObjectResult;
        await Assert.That(ok).IsNotNull();
        var resource = ok!.Value as HalResource<AiRunDto>;
        await Assert.That(resource).IsNotNull();
        await Assert.That(resource!.Links[LinkRelations.Self].Href).Contains($"/runs/{runId}");
        await Assert.That(resource.Links[LinkRelations.Up].Href).Contains($"/conversations/{conversationId}");
    }

    [Test]
    public async Task AiConversationHalResource_SerializesLinksWithSourceGeneratedContext()
    {
        var conversationId = Guid.CreateVersion7();
        var resource = new HalResource<AiConversationDto>(CreateDetail(conversationId, "Active"));
        resource.WithLink(LinkRelations.Self, HalLink.Create($"/api/ai/assistant/conversations/{conversationId}"));
        resource.WithLink(LinkRelations.SendMessage, HalLink.CreateAction($"/api/ai/assistant/conversations/{conversationId}/messages", "POST"));

        var json = JsonSerializer.Serialize(resource, ExploreJsonContext.Default.Options);

        await Assert.That(json).Contains("_links");
        await Assert.That(json).Contains("send-message");
        await Assert.That(json).Contains("POST");
        await Assert.That(json).Contains(conversationId.ToString());
    }

    private AiAssistantController CreateController()
    {
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "trace-ai-test"
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", Guid.CreateVersion7().ToString())], "test"));
        httpContext.Items["CorrelationId"] = "correlation-ai-test";

        return new AiAssistantController(_mediator, _linkGenerator, _conversationAssembler)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private static AiConversationSummaryDto CreateSummary(Guid id, string status) =>
        new()
        {
            Id = id,
            TenantId = Guid.CreateVersion7(),
            UserId = Guid.CreateVersion7(),
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

    private static AiConversationDto CreateDetail(Guid id, string status) =>
        new()
        {
            Id = id,
            TenantId = Guid.CreateVersion7(),
            UserId = Guid.CreateVersion7(),
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

    private static BaseCommandResponse<Guid> Success(Guid id) =>
        new()
        {
            Id = id,
            Success = true,
            Message = "OK"
        };

    private static BaseCommandResponse<Guid> Failure(string message, string failureCode) =>
        new()
        {
            Id = Guid.Empty,
            Success = false,
            Message = message,
            FailureCode = failureCode,
            Errors = [message]
        };

    private static T RouteValue<T>(object? routeValues, string key)
    {
        if (routeValues is RouteValueDictionary dictionary && dictionary.TryGetValue(key, out var dictionaryValue))
        {
            return (T)dictionaryValue!;
        }

        var value = routeValues?.GetType().GetProperty(key)?.GetValue(routeValues);
        return value is null ? default! : (T)value;
    }

    private static void AssertRoute(string methodName, Type attributeType, string template, string routeName)
    {
        var method = typeof(AiAssistantController).GetMethod(methodName)!;
        var attribute = method.GetCustomAttributes<HttpMethodAttribute>()
            .Single(candidate => candidate.GetType() == attributeType);

        Assert.That(attribute.Template).IsEqualTo(template);
        Assert.That(attribute.Name).IsEqualTo(routeName);
    }
}
