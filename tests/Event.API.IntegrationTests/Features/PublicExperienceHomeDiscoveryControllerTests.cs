// ABOUTME: API contract coverage for the anonymous composite public-home discovery endpoint.
// ABOUTME: Verifies stable routing, cache metadata, area/mode dispatch, and response typing.

using System.Reflection;
using Explore.API.Attributes;
using Explore.API.Controllers;
using Explore.API.Hateoas;
using Explore.Application.DTOs.PublicExperience;
using Explore.Application.Features.PublicExperience.Requests.Queries;
using Explore.Application.Models.PublicExperience;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.OutputCaching;
using NSubstitute;

namespace Explore.Api.IntegrationTests.Features;

public sealed class PublicExperienceHomeDiscoveryControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    [Test]
    public async Task HomeDiscoveryRouteHasStableAnonymousCachedMetadata()
    {
        var action = typeof(PublicExperienceController)
            .GetMethod(nameof(PublicExperienceController.GetHomeDiscovery))!;
        var route = action.GetCustomAttribute<HttpGetAttribute>();

        await Assert.That(route).IsNotNull();
        await Assert.That(route!.Template).IsEqualTo("~/api/public-experience/home");
        await Assert.That(route.Name).IsEqualTo(RouteNames.GetHomeDiscovery);
        await Assert.That(action.GetCustomAttribute<AllowAnonymousAttribute>()).IsNotNull();
        await Assert.That(action.GetCustomAttribute<EndpointClassificationAttribute>()?.Class).IsEqualTo(EndpointClass.Public);
        await Assert.That(action.GetCustomAttribute<OutputCacheAttribute>()?.PolicyName).IsEqualTo("PublicHomeDiscovery");
    }

    [Test]
    public async Task HomeDiscoveryDispatchesAreaAndModeQuery()
    {
        var areaId = Guid.NewGuid();
        var expected = new HomeDiscoveryDto
        {
            Context = new HomeDiscoveryContextDto
            {
                Mode = HomeDiscoveryMode.Online,
                SelectedAreaId = areaId
            }
        };
        _mediator.Send(Arg.Any<GetHomeDiscoveryQuery>(), Arg.Any<CancellationToken>())
            .Returns(expected);
        var controller = new PublicExperienceController(_mediator);

        var action = await controller.GetHomeDiscovery(areaId, "online", CancellationToken.None);

        var ok = action.Result as OkObjectResult;
        await Assert.That(ok).IsNotNull();
        await Assert.That(ok!.Value).IsSameReferenceAs(expected);
        await _mediator.Received(1).Send(
            Arg.Is<GetHomeDiscoveryQuery>(query =>
                query != null && query.AreaId == areaId && query.Mode == "online"),
            Arg.Any<CancellationToken>());
    }
}
