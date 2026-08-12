// ABOUTME: Controller contract tests for anonymous capability-scoped and authenticated registration-order APIs.
// ABOUTME: Verifies canonical routes, PublicTransactional safeguards, and token-safe HTTP transport.

using System.Reflection;
using System.Text.Json;
using Explore.API.Attributes;
using Explore.API.Controllers;
using Explore.API.Extensions;
using Explore.API.Filters;
using Explore.API.Hateoas;
using Explore.API.Models;
using Explore.API.OpenApi;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.RegistrationOrders;
using Explore.Application.DTOs.RegistrationSubmissions;
using Explore.Application.Features.RegistrationOrders.Requests.Commands;
using Explore.Application.Features.RegistrationOrders.Requests.Queries;
using Explore.Application.Features.RegistrationSubmissions.Commands;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using NSubstitute;

namespace Event.Api.IntegrationTests.Features;

public sealed class RegistrationOrderControllerTests
{
    private const string CapabilityHeader = "X-Registration-Order-Capability";

    [Test]
    public async Task GuestRoutes_UseCapabilityScopedPublicTransactionalContracts()
    {
        var controller = typeof(RegistrationOrderController);

        await AssertRoute<RegistrationOrderController, HttpPostAttribute>(
            nameof(RegistrationOrderController.StartGuest), "guest", RouteNames.StartGuestRegistrationOrder,
            EndpointClass.PublicTransactional, requiresIdempotency: true);
        await AssertRoute<RegistrationOrderController, HttpGetAttribute>(
            nameof(RegistrationOrderController.GetGuest), "guest/{orderId:guid}", RouteNames.GetGuestRegistrationOrder,
            EndpointClass.Public, requiresIdempotency: false);
        await AssertRoute<RegistrationOrderController, HttpPostAttribute>(
            nameof(RegistrationOrderController.ContinueGuest), "guest/{orderId:guid}/continue", RouteNames.ContinueGuestRegistrationOrder,
            EndpointClass.PublicTransactional, requiresIdempotency: true);
        await AssertRoute<RegistrationOrderController, HttpPostAttribute>(
            nameof(RegistrationOrderController.FinalizeGuest), "guest/{orderId:guid}/finalize", RouteNames.FinalizeGuestRegistrationOrder,
            EndpointClass.PublicTransactional, requiresIdempotency: true);
        await AssertRoute<RegistrationOrderController, HttpDeleteAttribute>(
            nameof(RegistrationOrderController.CancelGuest), "guest/{orderId:guid}", RouteNames.CancelGuestRegistrationOrder,
            EndpointClass.PublicTransactional, requiresIdempotency: true);

        await AssertRoute<RegistrationOrderController, HttpPostAttribute>(
            nameof(RegistrationOrderController.ClaimGuest), "guest/{orderId:guid}/claim", RouteNames.ClaimGuestRegistrationOrder,
            EndpointClass.Authenticated, requiresIdempotency: true);

        MethodInfo guestRead = controller.GetMethod(nameof(RegistrationOrderController.GetGuest))!;
        await Assert.That(guestRead.GetCustomAttribute<PrivateNoStoreAttribute>()).IsNotNull();
        ParameterInfo capability = guestRead.GetParameters()
            .Single(parameter => parameter.GetCustomAttribute<FromHeaderAttribute>()?.Name == CapabilityHeader);
        await Assert.That(capability.ParameterType).IsEqualTo(typeof(string));
    }

    [Test]
    public async Task AuthenticatedRoutes_UseCurrentAccountContracts()
    {
        await AssertRoute<RegistrationOrderController, HttpPostAttribute>(
            nameof(RegistrationOrderController.StartAuthenticated), string.Empty, RouteNames.StartAuthenticatedRegistrationOrder,
            EndpointClass.Authenticated, requiresIdempotency: false);
        await AssertRoute<RegistrationOrderController, HttpGetAttribute>(
            nameof(RegistrationOrderController.GetCurrent), "{orderId:guid}", RouteNames.GetCurrentRegistrationOrder,
            EndpointClass.Authenticated, requiresIdempotency: false);
        await AssertRoute<RegistrationOrderController, HttpPostAttribute>(
            nameof(RegistrationOrderController.ContinueAuthenticated), "{orderId:guid}/continue", RouteNames.ContinueAuthenticatedRegistrationOrder,
            EndpointClass.Authenticated, requiresIdempotency: false);
        await AssertRoute<RegistrationOrderController, HttpPostAttribute>(
            nameof(RegistrationOrderController.FinalizeAuthenticated), "{orderId:guid}/finalize", RouteNames.FinalizeAuthenticatedRegistrationOrder,
            EndpointClass.Authenticated, requiresIdempotency: false);
        await AssertRoute<RegistrationOrderController, HttpDeleteAttribute>(
            nameof(RegistrationOrderController.CancelAuthenticated), "{orderId:guid}", RouteNames.CancelAuthenticatedRegistrationOrder,
            EndpointClass.Authenticated, requiresIdempotency: false);
        await AssertRoute<RegistrationOrderController, HttpGetAttribute>(
            nameof(RegistrationOrderController.GetAuthenticatedParticipants), "{orderId:guid}/participants", RouteNames.GetAuthenticatedRegistrationOrderParticipants,
            EndpointClass.Authenticated, requiresIdempotency: false);
        await AssertRoute<RegistrationOrderController, HttpPostAttribute>(
            nameof(RegistrationOrderController.AddAuthenticatedParticipant), "{orderId:guid}/participants", RouteNames.AddAuthenticatedRegistrationOrderParticipant,
            EndpointClass.Authenticated, requiresIdempotency: true);
    }

    [Test]
    public async Task NativeSubmissionRoutes_ExposeAuthenticatedAndGuestTransactionalContracts()
    {
        await AssertNativeSubmissionRoute("LaunchAuthenticatedNativeAttempt", "{orderId:guid}/attempts", EndpointClass.Authenticated);
        await AssertNativeSubmissionRoute("SubmitAuthenticatedNativeAttempt", "{orderId:guid}/attempts/{attemptId:guid}/submissions", EndpointClass.Authenticated);
        await AssertNativeSubmissionRoute("LaunchGuestNativeAttempt", "guest/{orderId:guid}/attempts", EndpointClass.PublicTransactional);
        await AssertNativeSubmissionRoute("SubmitGuestNativeAttempt", "guest/{orderId:guid}/attempts/{attemptId:guid}/submissions", EndpointClass.PublicTransactional);
    }

    [Test]
    public async Task AttemptLaunchRequests_ForwardOptionalSupersededAttemptForExplicitRestart()
    {
        var mediator = Substitute.For<IMediator>();
        Guid oldAttemptId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid orderId = Guid.CreateVersion7();
        Guid requirementId = Guid.CreateVersion7();
        Guid channelId = Guid.CreateVersion7();
        Guid formId = Guid.CreateVersion7();
        Guid versionId = Guid.CreateVersion7();
        Guid bindingId = Guid.CreateVersion7();
        mediator.Send(Arg.Any<LaunchAuthenticatedNativeRegistrationAttemptCommand>(), Arg.Any<CancellationToken>())
            .Returns(new NativeRegistrationAttemptResult(true, Guid.CreateVersion7(), requirementId, channelId, formId, versionId,
                DateTime.UtcNow.AddMinutes(10), new NativeRegistrationFormDefinitionDto(versionId, 1, "en", null, [], []), [],
                new NativeRegistrationRequirementProgressDto(0, 0, 0, 0, false), false, "raw-token"));
        mediator.Send(Arg.Any<LaunchAuthenticatedRegistrationProviderAttemptCommand>(), Arg.Any<CancellationToken>())
            .Returns(new RegistrationProviderAttemptResult(true, Guid.CreateVersion7(), new NativeRegistrationProviderLaunchDescriptorDto(
                Guid.CreateVersion7(), requirementId, channelId, bindingId, formId, versionId, "redirect", true,
                "https://forms.example.test/start", "Provider registration", true, "manual", "ok", [],
                new NativeRegistrationRequirementProgressDto(0, 0, 0, 0, false))));
        var controller = CreateController(mediator);

        await controller.LaunchAuthenticatedNativeAttempt(eventId, orderId, "idem", new LaunchNativeRegistrationAttemptRequest(
            requirementId, channelId, formId, versionId, null, oldAttemptId));
        await controller.LaunchAuthenticatedProviderAttempt(eventId, orderId, new LaunchRegistrationProviderAttemptRequest(
            requirementId, channelId, bindingId, formId, versionId, oldAttemptId));

        _ = mediator.Received(1).Send(Arg.Is<LaunchAuthenticatedNativeRegistrationAttemptCommand>(command =>
            command.SupersededAttemptId == oldAttemptId), Arg.Any<CancellationToken>());
        _ = mediator.Received(1).Send(Arg.Is<LaunchAuthenticatedRegistrationProviderAttemptCommand>(command =>
            command.SupersededAttemptId == oldAttemptId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GuestParticipantRoutes_KeepCapabilityInHeaderAndWritesTransactional()
    {
        await AssertRoute<RegistrationOrderController, HttpGetAttribute>(
            nameof(RegistrationOrderController.GetGuestParticipants), "guest/{orderId:guid}/participants", RouteNames.GetGuestRegistrationOrderParticipants,
            EndpointClass.Public, requiresIdempotency: false);
        await AssertRoute<RegistrationOrderController, HttpPostAttribute>(
            nameof(RegistrationOrderController.AddGuestParticipant), "guest/{orderId:guid}/participants", RouteNames.AddGuestRegistrationOrderParticipant,
            EndpointClass.PublicTransactional, requiresIdempotency: true);

        MethodInfo read = typeof(RegistrationOrderController).GetMethod(nameof(RegistrationOrderController.GetGuestParticipants))!;
        ParameterInfo capability = read.GetParameters()
            .Single(parameter => parameter.GetCustomAttribute<FromHeaderAttribute>()?.Name == CapabilityHeader);
        await Assert.That(capability.ParameterType).IsEqualTo(typeof(string));
        await Assert.That(read.GetCustomAttribute<PrivateNoStoreAttribute>()).IsNotNull();
    }

    [Test]
    public async Task EventOrderCollectionRoute_UsesPrivateEventScopedOrganizerContract()
    {
        MethodInfo? action = typeof(RegistrationOrderController).GetMethod("GetEventOrders");

        await Assert.That(action).IsNotNull();
        var route = action!.GetCustomAttribute<HttpGetAttribute>();
        await Assert.That(route).IsNotNull();
        await Assert.That(route!.Template).IsEqualTo(string.Empty);
        await Assert.That(route.Name).IsEqualTo(RouteNames.GetEventRegistrationOrders);
        await Assert.That(action.GetCustomAttribute<AuthorizeAttribute>()).IsNotNull();
        await Assert.That(action.GetCustomAttribute<EndpointClassificationAttribute>()?.Class)
            .IsEqualTo(EndpointClass.Authenticated);
        await Assert.That(action.GetCustomAttribute<PrivateNoStoreAttribute>()).IsNotNull();
        await Assert.That(action.GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Any(attribute => attribute.StatusCode == StatusCodes.Status200OK &&
                              attribute.Type == typeof(HalCollectionResource<RegistrationOrderDto>))).IsTrue();
    }

    [Test]
    public async Task GetEventOrders_DispatchesEventScopedQueryAndAssemblesHalCollection()
    {
        var mediator = Substitute.For<IMediator>();
        var assembler = Substitute.For<IResourceAssembler<RegistrationOrderDto, RegistrationOrderDto>>();
        var eventId = Guid.CreateVersion7();
        IReadOnlyList<RegistrationOrderDto> orders = [new RegistrationOrderDto { Id = Guid.CreateVersion7(), EventId = eventId }];
        var collection = new HalCollectionResource<RegistrationOrderDto>();
        mediator.Send(Arg.Any<GetEventRegistrationOrdersQuery>(), Arg.Any<CancellationToken>()).Returns(orders);
        assembler.ToCollectionResource(
                Arg.Any<IEnumerable<RegistrationOrderDto>>(),
                RouteNames.GetEventRegistrationOrders,
                Arg.Any<object?>(),
                Arg.Any<HttpContext>())
            .Returns(collection);
        var controller = CreateController(mediator, assembler);

        ActionResult<HalCollectionResource<RegistrationOrderDto>> result = await controller.GetEventOrders(eventId);

        await Assert.That((result.Result as OkObjectResult)?.Value).IsEqualTo(collection);
        _ = mediator.Received(1).Send(
            Arg.Is<GetEventRegistrationOrdersQuery>(query => query.EventId == eventId),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GuestPostOpenApiMetadata_RequiresIdempotencyKey()
    {
        foreach (string actionName in new[]
                 {
                     nameof(RegistrationOrderController.StartGuest),
                     nameof(RegistrationOrderController.ContinueGuest),
                     nameof(RegistrationOrderController.FinalizeGuest)
                 })
        {
            MethodInfo action = typeof(RegistrationOrderController).GetMethod(actionName)!;
            var operation = new OpenApiOperation();

            bool applied = EndpointClassificationTransformer.ApplyIdempotencyKeyRequirement(
                operation,
                action.GetCustomAttributes().Cast<object>());

            await Assert.That(applied).IsTrue();
            await Assert.That(operation.Extensions).ContainsKey("x-idempotency-key-required");
        }
    }

    [Test]
    public async Task StartGuest_EmitsCapabilityOnlyInResponseHeader()
    {
        var token = "opaque-guest-capability";
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<StartGuestRegistrationOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GuestRegistrationOrderStartDto
            {
                Id = Guid.CreateVersion7(),
                Success = true,
                GuestCapabilityToken = token
            }));
        var controller = CreateController(mediator);
        var request = CreateStartRequest(platformContributionBasisPoints: 500);
        var eventId = Guid.CreateVersion7();

        ActionResult<GuestRegistrationOrderStartDto> result = await controller.StartGuest(eventId, request);

        var created = result.Result as CreatedAtRouteResult;
        await Assert.That(created).IsNotNull();
        await Assert.That(controller.Response.Headers[CapabilityHeader].ToString()).IsEqualTo(token);
        await Assert.That(JsonSerializer.Serialize(created!.Value)).DoesNotContain(token);
        _ = mediator.Received(1).Send(
            Arg.Is<StartGuestRegistrationOrderCommand>(command => command.PlatformContributionBasisPoints == 500),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GuestAccessFailures_ReturnGenericNotFoundWithoutCapabilityDisclosure()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetGuestRegistrationOrderQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<GuestRegistrationOrderDto?>(null));
        var controller = CreateController(mediator);

        ActionResult<HalResource<GuestRegistrationOrderDto>> result = await controller.GetGuest(
            Guid.CreateVersion7(), Guid.CreateVersion7(), "guessed-capability");

        var notFound = result.Result as ObjectResult;
        await Assert.That(notFound?.StatusCode).IsEqualTo(StatusCodes.Status404NotFound);
        await Assert.That(JsonSerializer.Serialize(notFound?.Value)).DoesNotContain("guessed-capability");
    }

    [Test]
    public async Task ParticipantHalMutationRelationsFollowServerManageDecision()
    {
        var mediator = Substitute.For<IMediator>();
        var eventId = Guid.CreateVersion7();
        var orderId = Guid.CreateVersion7();
        var viewOnly = new RegistrationOrderParticipantsDto(orderId, [], []);
        var manageable = viewOnly with { CanManage = true };
        mediator.Send(Arg.Any<GetAuthenticatedRegistrationOrderParticipantsQuery>(), Arg.Any<CancellationToken>())
            .Returns(viewOnly, manageable);
        var controller = CreateController(mediator);

        var organizerResult = await controller.GetAuthenticatedParticipants(eventId, orderId);
        var ownerResult = await controller.GetAuthenticatedParticipants(eventId, orderId);
        var organizerResource = (organizerResult.Result as OkObjectResult)?.Value as HalResource<RegistrationOrderParticipantsDto>;
        var ownerResource = (ownerResult.Result as OkObjectResult)?.Value as HalResource<RegistrationOrderParticipantsDto>;

        await Assert.That(organizerResource!.Links.Keys).IsEquivalentTo([LinkRelations.Self]);
        await Assert.That(ownerResource!.Links.Keys).Contains(LinkRelations.AddParticipant);
        await Assert.That(ownerResource.Links.Keys).Contains(LinkRelations.UpdateParticipant);
        await Assert.That(ownerResource.Links.Keys).Contains(LinkRelations.AssignTickets);
        await Assert.That(ownerResource.Links.Keys).Contains(LinkRelations.DeferTickets);
    }

    [Test]
    public async Task ContinueGuest_ForwardsContributionSelectionWithoutClientAmount()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<ContinueGuestRegistrationOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(new GuestRegistrationOrderLifecycleResponseDto { Id = Guid.CreateVersion7(), Success = true });
        var controller = CreateController(mediator);

        await controller.ContinueGuest(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "guest-token",
            new ContinueRegistrationOrderRequest { PlatformContributionBasisPoints = 500 });

        _ = mediator.Received(1).Send(
            Arg.Is<ContinueGuestRegistrationOrderCommand>(command => command.PlatformContributionBasisPoints == 500),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AccountRequiredGuestStart_ReturnsAuthenticationRequiredWithoutCreatingAnAccount()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<StartGuestRegistrationOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GuestRegistrationOrderStartDto
            {
                Id = Guid.CreateVersion7(),
                Success = false,
                FailureCode = "registration_order_identity_required",
                Message = "Registration order requires an authenticated account."
            }));
        var controller = CreateController(mediator);

        ActionResult<GuestRegistrationOrderStartDto> result = await controller.StartGuest(
            Guid.CreateVersion7(), CreateStartRequest());

        var unauthorized = result.Result as ObjectResult;
        await Assert.That(unauthorized?.StatusCode).IsEqualTo(StatusCodes.Status401Unauthorized);
        await Assert.That(JsonSerializer.Serialize(unauthorized?.Value)).DoesNotContain("registration_order_identity_required");
    }

    [Test]
    public async Task AuthenticatedReadAndLifecycle_AssembleHalResources()
    {
        var mediator = Substitute.For<IMediator>();
        var assembler = Substitute.For<IResourceAssembler<RegistrationOrderDto, RegistrationOrderDto>>();
        var order = CreateOrder();
        var resource = new HalResource<RegistrationOrderDto>(order);
        mediator.Send(Arg.Any<GetCurrentRegistrationOrderQuery>(), Arg.Any<CancellationToken>()).Returns(order);
        mediator.Send(Arg.Any<ContinueAuthenticatedRegistrationOrderCommand>(), Arg.Any<CancellationToken>()).Returns(
            new RegistrationOrderLifecycleResponseDto { Id = order.Id, Success = true, Order = order });
        mediator.Send(Arg.Any<FinalizeAuthenticatedRegistrationOrderCommand>(), Arg.Any<CancellationToken>()).Returns(
            new RegistrationOrderLifecycleResponseDto { Id = order.Id, Success = true, Order = order });
        mediator.Send(Arg.Any<CancelAuthenticatedRegistrationOrderCommand>(), Arg.Any<CancellationToken>()).Returns(
            new RegistrationOrderLifecycleResponseDto { Id = order.Id, Success = true, Order = order });
        assembler.ToResource(order, Arg.Any<HttpContext>()).Returns(resource);
        var controller = CreateController(mediator, assembler);

        var current = await controller.GetCurrent(order.EventId, order.Id);
        var continued = await controller.ContinueAuthenticated(order.EventId, order.Id);
        var finalized = await controller.FinalizeAuthenticated(order.EventId, order.Id);
        var cancelled = await controller.CancelAuthenticated(order.EventId, order.Id);

        foreach (ActionResult<HalResource<RegistrationOrderDto>> result in new[] { current, continued, finalized, cancelled })
        {
            var objectResult = result.Result as ObjectResult;
            await Assert.That(objectResult?.Value).IsEqualTo(resource);
            await Assert.That(objectResult?.ContentTypes).Contains(HateoasConstants.HalJsonMediaType);
        }

        await assembler.Received(4).ToResource(order, Arg.Any<HttpContext>());
    }

    [Test]
    public async Task GuestRead_ReturnsCapabilityScopedHalWithoutAuthenticatedAssembler()
    {
        var mediator = Substitute.For<IMediator>();
        var assembler = Substitute.For<IResourceAssembler<RegistrationOrderDto, RegistrationOrderDto>>();
        var guestOrder = new GuestRegistrationOrderDto { Id = Guid.CreateVersion7(), EventId = Guid.CreateVersion7() };
        mediator.Send(Arg.Any<GetGuestRegistrationOrderQuery>(), Arg.Any<CancellationToken>()).Returns(guestOrder);
        var controller = CreateController(mediator, assembler);

        ActionResult<HalResource<GuestRegistrationOrderDto>> result = await controller.GetGuest(
            guestOrder.EventId, guestOrder.Id, "opaque-capability");

        var resource = (result.Result as OkObjectResult)?.Value as HalResource<GuestRegistrationOrderDto>;
        await Assert.That(resource?.Data).IsEqualTo(guestOrder);
        await Assert.That(resource?.Links).ContainsKey(LinkRelations.Self);
        await Assert.That(resource?.Links).ContainsKey(LinkRelations.ClaimRegistrationOrder);
        await assembler.DidNotReceive().ToResource(Arg.Any<RegistrationOrderDto>(), Arg.Any<HttpContext>());
    }

    [Test]
    public async Task ClaimGuest_DispatchesCapabilityScopedCommandAndMapsConflict()
    {
        var mediator = Substitute.For<IMediator>();
        var orderId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        mediator.Send(Arg.Any<ClaimGuestRegistrationOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponse<Guid>
            {
                Id = orderId,
                Success = false,
                FailureCode = "registration_order_already_linked",
                Message = "Registration order is already linked to another account."
            });
        var controller = CreateController(mediator);

        ActionResult<BaseCommandResponse<Guid>> result = await controller.ClaimGuest(eventId, orderId, "guest-token");

        var conflict = result.Result as ConflictObjectResult;
        await Assert.That(conflict).IsNotNull();
        _ = mediator.Received(1).Send(
            Arg.Is<ClaimGuestRegistrationOrderCommand>(command =>
                command.EventId == eventId && command.OrderId == orderId && command.CapabilityToken == "guest-token"),
            Arg.Any<CancellationToken>());
    }

    private static RegistrationOrderController CreateController(
        IMediator mediator,
        IResourceAssembler<RegistrationOrderDto, RegistrationOrderDto>? assembler = null)
    {
        var controller = new RegistrationOrderController(
            mediator,
            assembler ?? Substitute.For<IResourceAssembler<RegistrationOrderDto, RegistrationOrderDto>>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var url = Substitute.For<IUrlHelper>();
        url.Link(Arg.Any<string>(), Arg.Any<object>()).Returns(call => $"/api/routes/{call.ArgAt<string>(0)}");
        controller.Url = url;

        return controller;
    }

    private static RegistrationOrderDto CreateOrder() => new()
    {
        Id = Guid.CreateVersion7(),
        EventId = Guid.CreateVersion7()
    };

    private static async Task AssertNativeSubmissionRoute(
        string actionName,
        string routeTemplate,
        EndpointClass endpointClass)
    {
        MethodInfo? action = typeof(RegistrationOrderController).GetMethod(actionName);
        await Assert.That(action).IsNotNull();
        var route = action!.GetCustomAttribute<HttpPostAttribute>();
        await Assert.That(route?.Template).IsEqualTo(routeTemplate);
        await Assert.That(action.GetCustomAttribute<EndpointClassificationAttribute>()?.Class).IsEqualTo(endpointClass);
        await Assert.That(action.GetCustomAttribute<RequireIdempotencyKeyAttribute>()).IsNotNull();
        await Assert.That(action.GetCustomAttribute<AuthorizeAttribute>() is not null)
            .IsEqualTo(endpointClass == EndpointClass.Authenticated);
    }

    private static StartRegistrationOrderRequest CreateStartRequest(int? platformContributionBasisPoints = null) => new()
    {
        TicketCatalogVersionId = Guid.CreateVersion7(),
        BookingPartyType = BookingPartyTypeEnum.Individual,
        Lines = [new RegistrationOrderLineSelection(Guid.CreateVersion7(), 1, null)],
        PlatformContributionBasisPoints = platformContributionBasisPoints
    };

    private static async Task AssertRoute<TController, TAttribute>(
        string actionName,
        string template,
        string routeName,
        EndpointClass endpointClass,
        bool requiresIdempotency)
        where TController : ControllerBase
        where TAttribute : HttpMethodAttribute
    {
        MethodInfo action = typeof(TController).GetMethod(actionName)!;
        var route = action.GetCustomAttribute<TAttribute>();

        await Assert.That(route).IsNotNull();
        await Assert.That(route!.Template).IsEqualTo(template);
        await Assert.That(route.Name).IsEqualTo(routeName);
        await Assert.That(action.GetCustomAttribute<EndpointClassificationAttribute>()?.Class).IsEqualTo(endpointClass);

        if (endpointClass == EndpointClass.PublicTransactional)
        {
            await Assert.That(action.GetCustomAttribute<AllowAnonymousAttribute>()).IsNotNull();
            await Assert.That(action.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName)
                .IsEqualTo(RateLimitingExtensions.PublicTransactionalPolicy);
        }
        else if (endpointClass == EndpointClass.Authenticated)
        {
            await Assert.That(action.GetCustomAttribute<AuthorizeAttribute>()).IsNotNull();
        }
        else
        {
            await Assert.That(action.GetCustomAttribute<AllowAnonymousAttribute>()).IsNotNull();
        }

        await Assert.That(action.GetCustomAttribute<RequireIdempotencyKeyAttribute>() is not null)
            .IsEqualTo(requiresIdempotency);
        await Assert.That(action.GetCustomAttributes<ProducesResponseTypeAttribute>()).IsNotEmpty();
    }
}
