// ABOUTME: Unit tests for HateoasAuthorizationEvaluator verifying dedup, fail-closed, and static check behavior.
// ABOUTME: Uses NSubstitute to mock IAuthorizationProvider and validates the 4-phase authorization pipeline.

namespace Event.Application.UnitTests.Hateoas;

using System.Security.Claims;
using Explore.API.Hateoas;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Hateoas;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TUnit.Assertions;
using TUnit.Core;

public class HateoasAuthorizationEvaluatorTests
{
    private readonly IAuthorizationProvider _authProvider = Substitute.For<IAuthorizationProvider>();
    private readonly ILogger<HateoasAuthorizationEvaluator> _logger = Substitute.For<ILogger<HateoasAuthorizationEvaluator>>();
    private readonly HateoasAuthorizationEvaluator _sut;
    private readonly HttpContext _httpContext = new DefaultHttpContext();

    public HateoasAuthorizationEvaluatorTests()
    {
        _sut = new HateoasAuthorizationEvaluator(_authProvider, _logger);
    }

    private static ClaimsPrincipal AuthenticatedUser(params string[] roles)
    {
        var claims = new List<Claim> { new("sub", Guid.NewGuid().ToString()) };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    [Test]
    [DisplayName("Empty link list returns empty results")]
    public async Task EmptyList_ReturnsEmpty()
    {
        var result = await _sut.AreLinksAllowedAsync([], null, _httpContext);

        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    [DisplayName("Links without permission requirements pass through")]
    public async Task LinksWithoutPermission_AllAllowed()
    {
        var links = new List<LinkDefinition>
        {
            LinkDefinition.Self("GetEvent"),
            LinkDefinition.Collection("GetEvents"),
        };

        var result = await _sut.AreLinksAllowedAsync(links, AuthenticatedUser(), _httpContext);

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0]).IsTrue();
        await Assert.That(result[1]).IsTrue();
    }

    [Test]
    [DisplayName("Permission-bound links are batch evaluated via provider")]
    public async Task PermissionLinks_BatchEvaluated()
    {
        var links = new List<LinkDefinition>
        {
            LinkDefinition.Edit("UpdateEvent")
                .WithPermission("event", "update", "id-1"),
        };

        _authProvider.IsAllowedBatchAsync(
            Arg.Any<IReadOnlyList<AuthorizationCheck>>(),
            Arg.Any<CancellationToken>())
            .Returns([true]);

        var result = await _sut.AreLinksAllowedAsync(links, AuthenticatedUser(), _httpContext);

        await Assert.That(result[0]).IsTrue();
        await _authProvider.Received(1).IsAllowedBatchAsync(
            Arg.Is<IReadOnlyList<AuthorizationCheck>>(checks => checks.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Test]
    [DisplayName("Duplicate checks are deduplicated — provider receives only unique checks")]
    public async Task DuplicateChecks_Deduplicated()
    {
        // Two links with identical resource kind + id + action should collapse to 1 check
        var links = new List<LinkDefinition>
        {
            LinkDefinition.Edit("UpdateEvent")
                .WithPermission("event", "update", "id-1"),
            LinkDefinition.Action("add-categories", "UpdateEventCategories", "PUT")
                .WithPermission("event", "update", "id-1"),
        };

        _authProvider.IsAllowedBatchAsync(
            Arg.Any<IReadOnlyList<AuthorizationCheck>>(),
            Arg.Any<CancellationToken>())
            .Returns([true]);

        var result = await _sut.AreLinksAllowedAsync(links, AuthenticatedUser(), _httpContext);

        await Assert.That(result[0]).IsTrue();
        await Assert.That(result[1]).IsTrue();
        await _authProvider.Received(1).IsAllowedBatchAsync(
            Arg.Is<IReadOnlyList<AuthorizationCheck>>(checks => checks.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Test]
    [DisplayName("Dedup maps batch decisions correctly back to all original links")]
    public async Task DedupMapsDecisions_Correctly()
    {
        // 3 links: 2 share same dedup key (event|id-1|update), 1 is different (event|id-1|delete)
        var links = new List<LinkDefinition>
        {
            LinkDefinition.Edit("UpdateEvent")
                .WithPermission("event", "update", "id-1"),
            LinkDefinition.Action("add-tags", "UpdateEventTags", "PUT")
                .WithPermission("event", "update", "id-1"),
            LinkDefinition.Delete("DeleteEvent")
                .WithPermission("event", "delete", "id-1"),
        };

        // Provider returns 2 decisions for 2 unique checks: update=allowed, delete=denied
        _authProvider.IsAllowedBatchAsync(
            Arg.Any<IReadOnlyList<AuthorizationCheck>>(),
            Arg.Any<CancellationToken>())
            .Returns([true, false]);

        var result = await _sut.AreLinksAllowedAsync(links, AuthenticatedUser(), _httpContext);

        await Assert.That(result[0]).IsTrue();  // update — allowed
        await Assert.That(result[1]).IsTrue();  // update (dedup'd) — same decision
        await Assert.That(result[2]).IsFalse(); // delete — denied
    }

    [Test]
    [DisplayName("Batch exception triggers fail-closed: permission-bound links denied, others unaffected")]
    public async Task BatchException_FailClosed()
    {
        var links = new List<LinkDefinition>
        {
            LinkDefinition.Self("GetEvent"),
            LinkDefinition.Edit("UpdateEvent")
                .WithPermission("event", "update", "id-1"),
            LinkDefinition.Delete("DeleteEvent")
                .WithPermission("event", "delete", "id-1"),
        };

        _authProvider.IsAllowedBatchAsync(
            Arg.Any<IReadOnlyList<AuthorizationCheck>>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Cerbos unreachable"));

        var result = await _sut.AreLinksAllowedAsync(links, AuthenticatedUser(), _httpContext);

        await Assert.That(result[0]).IsTrue();  // Self — no permission check, unaffected
        await Assert.That(result[1]).IsFalse(); // Edit — fail-closed
        await Assert.That(result[2]).IsFalse(); // Delete — fail-closed
    }

    [Test]
    [DisplayName("Condition returning false denies link without provider call")]
    public async Task ConditionFalse_Denied_NoProviderCall()
    {
        var links = new List<LinkDefinition>
        {
            LinkDefinition.Edit("UpdateEvent")
                .When(() => false)
                .WithPermission("event", "update", "id-1"),
        };

        var result = await _sut.AreLinksAllowedAsync(links, AuthenticatedUser(), _httpContext);

        await Assert.That(result[0]).IsFalse();
        await _authProvider.DidNotReceive().IsAllowedBatchAsync(
            Arg.Any<IReadOnlyList<AuthorizationCheck>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    [DisplayName("RequiresAuth with unauthenticated user denies link")]
    public async Task RequiresAuth_Unauthenticated_Denied()
    {
        // Edit sets RequiresAuth=true by default
        var links = new List<LinkDefinition>
        {
            LinkDefinition.Edit("UpdateEvent")
                .WithPermission("event", "update", "id-1"),
        };

        var result = await _sut.AreLinksAllowedAsync(links, null, _httpContext);

        await Assert.That(result[0]).IsFalse();
    }

    [Test]
    [DisplayName("Missing required role denies link")]
    public async Task MissingRole_Denied()
    {
        var links = new List<LinkDefinition>
        {
            LinkDefinition.Edit("UpdateEvent", roles: ["Admin"])
                .WithPermission("event", "update", "id-1"),
        };

        var result = await _sut.AreLinksAllowedAsync(links, AuthenticatedUser("User"), _httpContext);

        await Assert.That(result[0]).IsFalse();
    }

    [Test]
    [DisplayName("Having required role proceeds to permission evaluation")]
    public async Task HasRole_ProceedsToPermissionCheck()
    {
        var links = new List<LinkDefinition>
        {
            LinkDefinition.Edit("UpdateEvent", roles: ["Admin"])
                .WithPermission("event", "update", "id-1"),
        };

        _authProvider.IsAllowedBatchAsync(
            Arg.Any<IReadOnlyList<AuthorizationCheck>>(),
            Arg.Any<CancellationToken>())
            .Returns([true]);

        var result = await _sut.AreLinksAllowedAsync(links, AuthenticatedUser("Admin"), _httpContext);

        await Assert.That(result[0]).IsTrue();
    }

    [Test]
    [DisplayName("Mixed static and permission checks produce correct combined results")]
    public async Task MixedChecks_CorrectResults()
    {
        var links = new List<LinkDefinition>
        {
            LinkDefinition.Self("GetEvent"),                                   // No auth, no permission → true
            LinkDefinition.Edit("UpdateEvent")
                .When(() => false)
                .WithPermission("event", "update", "id-1"),                    // Condition false → false
            LinkDefinition.Delete("DeleteEvent")
                .WithPermission("event", "delete", "id-1"),                    // Permission check → depends on provider
            LinkDefinition.Create("CreateEvent")
                .WithPermission("event", "create", "event"),                   // Permission check → depends on provider
        };

        _authProvider.IsAllowedBatchAsync(
            Arg.Any<IReadOnlyList<AuthorizationCheck>>(),
            Arg.Any<CancellationToken>())
            .Returns([false, true]); // delete=denied, create=allowed

        var result = await _sut.AreLinksAllowedAsync(links, AuthenticatedUser(), _httpContext);

        await Assert.That(result[0]).IsTrue();  // Self — pass through
        await Assert.That(result[1]).IsFalse(); // Edit — condition false
        await Assert.That(result[2]).IsFalse(); // Delete — provider denied
        await Assert.That(result[3]).IsTrue();  // Create — provider allowed
    }
}
