// ABOUTME: Unit tests for SetupSecretRequiredAttribute filter behavior using a resolved private inner TypeFilter.
// ABOUTME: Verifies setup-mode checks, header secret validation, and action execution gating outcomes.

using Explore.API.Filters;
using Explore.Application.Contracts.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using NSubstitute;

namespace Event.Application.UnitTests.Infrastructure;

public class SetupSecretRequiredFilterTests
{
    [Test]
    public async Task OnActionExecutionAsync_SetupNotActive_ReturnsGone()
    {
        var setupSecretProvider = Substitute.For<ISetupSecretProvider>();
        setupSecretProvider.IsSetupModeActive.Returns(false);

        var filter = CreateFilter(setupSecretProvider);
        var context = CreateExecutingContext("valid-secret");
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, CreateNext(context, () => nextCalled = true));

        await Assert.That(nextCalled).IsEqualTo(false);
        await Assert.That(context.Result).IsTypeOf<ObjectResult>();
        var result = (ObjectResult)context.Result!;
        await Assert.That(result.StatusCode).IsEqualTo(StatusCodes.Status410Gone);
    }

    [Test]
    public async Task OnActionExecutionAsync_SetupActiveWrongSecret_ReturnsForbidden()
    {
        var setupSecretProvider = Substitute.For<ISetupSecretProvider>();
        setupSecretProvider.IsSetupModeActive.Returns(true);
        setupSecretProvider.ValidateSecret("wrong-secret").Returns(false);

        var filter = CreateFilter(setupSecretProvider);
        var context = CreateExecutingContext("wrong-secret");
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, CreateNext(context, () => nextCalled = true));

        await Assert.That(nextCalled).IsEqualTo(false);
        await Assert.That(context.Result).IsTypeOf<ObjectResult>();
        var result = (ObjectResult)context.Result!;
        await Assert.That(result.StatusCode).IsEqualTo(StatusCodes.Status403Forbidden);
    }

    [Test]
    public async Task OnActionExecutionAsync_SetupActiveValidSecret_CallsNext()
    {
        var setupSecretProvider = Substitute.For<ISetupSecretProvider>();
        setupSecretProvider.IsSetupModeActive.Returns(true);
        setupSecretProvider.ValidateSecret("valid-secret").Returns(true);

        var filter = CreateFilter(setupSecretProvider);
        var context = CreateExecutingContext("valid-secret");
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, CreateNext(context, () => nextCalled = true));

        await Assert.That(nextCalled).IsEqualTo(true);
        await Assert.That(context.Result).IsNull();
    }

    [Test]
    public async Task OnActionExecutionAsync_SetupActiveNoHeader_ReturnsForbidden()
    {
        var setupSecretProvider = Substitute.For<ISetupSecretProvider>();
        setupSecretProvider.IsSetupModeActive.Returns(true);
        setupSecretProvider.ValidateSecret(null).Returns(false);

        var filter = CreateFilter(setupSecretProvider);
        var context = CreateExecutingContext();
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, CreateNext(context, () => nextCalled = true));

        await Assert.That(nextCalled).IsEqualTo(false);
        await Assert.That(context.Result).IsTypeOf<ObjectResult>();
        var result = (ObjectResult)context.Result!;
        await Assert.That(result.StatusCode).IsEqualTo(StatusCodes.Status403Forbidden);
    }

    [Test]
    public async Task OnActionExecutionAsync_SetupActiveEmptyHeader_ReturnsForbidden()
    {
        var setupSecretProvider = Substitute.For<ISetupSecretProvider>();
        setupSecretProvider.IsSetupModeActive.Returns(true);
        setupSecretProvider.ValidateSecret(string.Empty).Returns(false);

        var filter = CreateFilter(setupSecretProvider);
        var context = CreateExecutingContext(string.Empty);
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, CreateNext(context, () => nextCalled = true));

        await Assert.That(nextCalled).IsEqualTo(false);
        await Assert.That(context.Result).IsTypeOf<ObjectResult>();
        var result = (ObjectResult)context.Result!;
        await Assert.That(result.StatusCode).IsEqualTo(StatusCodes.Status403Forbidden);
    }

    private static IAsyncActionFilter CreateFilter(ISetupSecretProvider setupSecretProvider)
    {
        var attribute = new SetupSecretRequiredAttribute();
        var filterType = attribute.ImplementationType;
        return (IAsyncActionFilter)Activator.CreateInstance(filterType, setupSecretProvider)!;
    }

    private static ActionExecutingContext CreateExecutingContext(string? setupSecretHeader = null)
    {
        var httpContext = new DefaultHttpContext();
        if (setupSecretHeader is not null)
        {
            httpContext.Request.Headers["X-Setup-Secret"] = setupSecretHeader;
        }

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), null!);
    }

    private static ActionExecutionDelegate CreateNext(ActionExecutingContext context, Action onCall)
    {
        return () =>
        {
            onCall();
            return Task.FromResult(new ActionExecutedContext(context, new List<IFilterMetadata>(), null!));
        };
    }
}
