// ABOUTME: Unit tests for HateoasAuthorizationEvaluator verifying dedup, fail-closed, and static check behavior.
// ABOUTME: Uses NSubstitute to mock IAuthorizationProvider and validates the 4-phase authorization pipeline.

namespace Event.Api.IntegrationTests.Features.Hateoas;

using System.Security.Claims;
using Explore.API.Hateoas;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Hateoas;
using Explore.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TUnit.Assertions;
using TUnit.Core;

public class HateoasAuthorizationEvaluatorTests
{
    private readonly IAuthorizationProvider _authProvider = Substitute.For<IAuthorizationProvider>();
    private readonly Explore.Application.Contracts.Persistence.IEventRepository _eventRepository =
        Substitute.For<Explore.Application.Contracts.Persistence.IEventRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ILogger<HateoasAuthorizationEvaluator> _logger = Substitute.For<ILogger<HateoasAuthorizationEvaluator>>();
    private readonly HateoasAuthorizationEvaluator _sut;
    private readonly HttpContext _httpContext = new DefaultHttpContext();

    public HateoasAuthorizationEvaluatorTests()
    {
        _sut = new HateoasAuthorizationEvaluator(_authProvider, _eventRepository, _tenantContext, _logger);
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
                .WithPermission("islamuevent_event", "update", "id-1"),
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
    public async Task RegistrationFormChecksReplaceSpoofedAuthorityFromOnePersistedEventLoad()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid organizerUserId = Guid.CreateVersion7();
        Guid attackerUserId = Guid.CreateVersion7();
        IEventRepository repository = Substitute.For<IEventRepository>();
        ITenantContext tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(tenantId);
        repository.GetAuthorizationTargetsByIdsAsync(
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 1 && ids.Contains(eventId)),
                Arg.Any<CancellationToken>())
            .Returns([AuthorizationEvent(tenantId, eventId, attackerUserId, organizerUserId)]);
        IReadOnlyList<AuthorizationCheck>? captured = null;
        _authProvider.IsAllowedBatchAsync(
                Arg.Any<IReadOnlyList<AuthorizationCheck>>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                captured = call.Arg<IReadOnlyList<AuthorizationCheck>>();
                return captured.Select(_ => true).ToArray();
            });
        var evaluator = new HateoasAuthorizationEvaluator(_authProvider, repository, tenantContext, _logger);
        Dictionary<string, object> spoofed = new()
        {
            ["eventId"] = eventId.ToString("D"),
            ["tenantId"] = Guid.CreateVersion7().ToString("D"),
            ["organizerUserId"] = attackerUserId.ToString("D")
        };
        LinkDefinition[] links =
        [
            LinkDefinition.Action("publish", "Publish", "POST").WithPermission(
                ResourceKinds.RegistrationForm, AuthorizationActions.RegistrationForms.Publish, "form", spoofed),
            LinkDefinition.Action("edit", "Edit", "PATCH").WithPermission(
                ResourceKinds.RegistrationForm, AuthorizationActions.RegistrationForms.Update, "form", spoofed),
            LinkDefinition.Action("delete", "Delete", "DELETE").WithPermission(
                ResourceKinds.RegistrationForm, AuthorizationActions.RegistrationForms.Delete, "form", spoofed)
        ];

        IReadOnlyList<bool> result = await evaluator.AreLinksAllowedAsync(links, AuthenticatedUser(), _httpContext);

        await Assert.That(result.All(value => value)).IsTrue();
        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.All(check =>
            check.ResourceAttributes!["tenantId"].Equals(tenantId.ToString("D")) &&
            check.ResourceAttributes["organizerUserId"].Equals(organizerUserId.ToString("D")) &&
            check.ResourceAttributes["userId"].Equals(attackerUserId.ToString("D")))).IsTrue();
        await repository.Received(1).GetAuthorizationTargetsByIdsAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RegistrationFormChecksFailClosedForMissingAndCrossTenantParents()
    {
        Guid eventId = Guid.CreateVersion7();
        IAuthorizationProvider authorizationProvider = Substitute.For<IAuthorizationProvider>();
        IEventRepository repository = Substitute.For<IEventRepository>();
        ITenantContext tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(Guid.CreateVersion7());
        repository.GetAuthorizationTargetsByIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns([AuthorizationEvent(Guid.CreateVersion7(), eventId, Guid.CreateVersion7(), Guid.CreateVersion7())]);
        var evaluator = new HateoasAuthorizationEvaluator(authorizationProvider, repository, tenantContext, _logger);
        LinkDefinition missing = RegistrationLink(new Dictionary<string, object>());
        LinkDefinition crossTenant = RegistrationLink(
            new Dictionary<string, object> { ["eventId"] = eventId.ToString("D") });

        IReadOnlyList<bool> result = await evaluator.AreLinksAllowedAsync(
            [missing, crossTenant],
            AuthenticatedUser(),
            _httpContext);

        await Assert.That(result.All(value => !value)).IsTrue();
        await authorizationProvider.DidNotReceive().IsAllowedBatchAsync(
            Arg.Any<IReadOnlyList<AuthorizationCheck>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RegistrationFormChecksFailClosedAboveRepositoryBatchBound()
    {
        IAuthorizationProvider authorizationProvider = Substitute.For<IAuthorizationProvider>();
        IEventRepository repository = Substitute.For<IEventRepository>();
        ITenantContext tenantContext = Substitute.For<ITenantContext>();
        var evaluator = new HateoasAuthorizationEvaluator(authorizationProvider, repository, tenantContext, _logger);
        LinkDefinition[] links = Enumerable.Range(0, IEventRepository.MaximumAuthorizationTargetBatchSize + 1)
            .Select(_ => RegistrationLink(new Dictionary<string, object>
            {
                ["eventId"] = Guid.CreateVersion7().ToString("D")
            }))
            .ToArray();

        IReadOnlyList<bool> result = await evaluator.AreLinksAllowedAsync(links, AuthenticatedUser(), _httpContext);

        await Assert.That(result.All(value => !value)).IsTrue();
        await repository.DidNotReceive().GetAuthorizationTargetsByIdsAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<CancellationToken>());
        await authorizationProvider.DidNotReceive().IsAllowedBatchAsync(
            Arg.Any<IReadOnlyList<AuthorizationCheck>>(),
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
                .WithPermission("islamuevent_event", "update", "id-1"),
            LinkDefinition.Action("add-categories", "UpdateEventCategories", "PUT")
                .WithPermission("islamuevent_event", "update", "id-1"),
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
    [DisplayName("Checks with different scopes are not deduplicated")]
    public async Task ChecksWithDifferentScopes_NotDeduplicated()
    {
        var tenantScope = new AuthorizationScope(TenantId: "tenant-1");
        var orgScope = new AuthorizationScope(TenantId: "tenant-1", OrganizationId: "org-1");
        var links = new List<LinkDefinition>
        {
            LinkDefinition.Edit("UpdateTenantEvent")
                .WithPermission("islamuevent_event", "update", "event-1", scope: tenantScope),
            LinkDefinition.Edit("UpdateOrganizationEvent")
                .WithPermission("islamuevent_event", "update", "event-1", scope: orgScope),
        };

        _authProvider.IsAllowedBatchAsync(
            Arg.Any<IReadOnlyList<AuthorizationCheck>>(),
            Arg.Any<CancellationToken>())
            .Returns([true, false]);

        var result = await _sut.AreLinksAllowedAsync(links, AuthenticatedUser(), _httpContext);

        await Assert.That(result[0]).IsTrue();
        await Assert.That(result[1]).IsFalse();
        await _authProvider.Received(1).IsAllowedBatchAsync(
            Arg.Is<IReadOnlyList<AuthorizationCheck>>(checks => ChecksContainScopes(checks, tenantScope, orgScope)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    [DisplayName("Checks with different resource attributes are not deduplicated")]
    public async Task ChecksWithDifferentAttributes_NotDeduplicated()
    {
        var links = new List<LinkDefinition>
        {
            LinkDefinition.Edit("UpdateTenantEvent")
                .WithPermission(
                    "islamuevent_event",
                    "update",
                    "event-1",
                    new Dictionary<string, object> { ["tenantId"] = "tenant-1", ["ownerId"] = "owner-1" }),
            LinkDefinition.Edit("UpdateOtherTenantEvent")
                .WithPermission(
                    "islamuevent_event",
                    "update",
                    "event-1",
                    new Dictionary<string, object> { ["tenantId"] = "tenant-2", ["ownerId"] = "owner-1" }),
        };

        _authProvider.IsAllowedBatchAsync(
            Arg.Any<IReadOnlyList<AuthorizationCheck>>(),
            Arg.Any<CancellationToken>())
            .Returns([true, false]);

        var result = await _sut.AreLinksAllowedAsync(links, AuthenticatedUser(), _httpContext);

        await Assert.That(result[0]).IsTrue();
        await Assert.That(result[1]).IsFalse();
        await _authProvider.Received(1).IsAllowedBatchAsync(
            Arg.Is<IReadOnlyList<AuthorizationCheck>>(checks => ChecksContainTenantAttributes(checks, "tenant-1", "tenant-2")),
            Arg.Any<CancellationToken>());
    }

    [Test]
    [DisplayName("Dedup key canonicalizes attributes independent of insertion order")]
    public async Task DedupKey_CanonicalizesAttributesIndependentOfInsertionOrder()
    {
        var first = new AuthorizationCheck(
            "islamuevent_event",
            "event-1",
            "update",
            new Dictionary<string, object> { ["tenantId"] = "tenant-1", ["ownerId"] = "owner-1" },
            AuthorizationScope.Empty);
        var second = new AuthorizationCheck(
            "islamuevent_event",
            "event-1",
            "update",
            new Dictionary<string, object> { ["ownerId"] = "owner-1", ["tenantId"] = "tenant-1" });

        await Assert.That(first.ToDeduplicationKey()).IsEqualTo(second.ToDeduplicationKey());
    }

    [Test]
    [DisplayName("Dedup key changes when resource attributes differ")]
    public async Task DedupKey_ChangesWhenAttributesDiffer()
    {
        var first = new AuthorizationCheck(
            "islamuevent_event",
            "event-1",
            "update",
            new Dictionary<string, object> { ["tenantId"] = "tenant-1" });
        var second = new AuthorizationCheck(
            "islamuevent_event",
            "event-1",
            "update",
            new Dictionary<string, object> { ["tenantId"] = "tenant-2" });

        await Assert.That(first.ToDeduplicationKey()).IsNotEqualTo(second.ToDeduplicationKey());
    }

    [Test]
    [DisplayName("Descriptor permission overload propagates scope to authorization provider")]
    public async Task DescriptorPermission_PropagatesScopeToProvider()
    {
        var resource = new TestResource("resource-1", "tenant-1", "org-1");
        var descriptor = new ResourceDescriptor<TestResource>(
            "islamuevent_event",
            static candidate => candidate.Id,
            static candidate => new Dictionary<string, object> { ["tenantId"] = candidate.TenantId },
            static candidate => new AuthorizationScope(candidate.TenantId, candidate.OrganizationId));
        var links = new List<LinkDefinition>
        {
            LinkDefinition.Edit("UpdateResource")
                .RequirePermission("update", descriptor, resource),
        };

        _authProvider.IsAllowedBatchAsync(
            Arg.Any<IReadOnlyList<AuthorizationCheck>>(),
            Arg.Any<CancellationToken>())
            .Returns([true]);

        var result = await _sut.AreLinksAllowedAsync(links, AuthenticatedUser(), _httpContext);

        await Assert.That(result[0]).IsTrue();
        await _authProvider.Received(1).IsAllowedBatchAsync(
            Arg.Is<IReadOnlyList<AuthorizationCheck>>(checks => CheckContainsScopeAndAttributes(checks, "tenant-1", "org-1")),
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
                .WithPermission("islamuevent_event", "update", "id-1"),
            LinkDefinition.Action("add-tags", "UpdateEventTags", "PUT")
                .WithPermission("islamuevent_event", "update", "id-1"),
            LinkDefinition.Delete("DeleteEvent")
                .WithPermission("islamuevent_event", "delete", "id-1"),
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
                .WithPermission("islamuevent_event", "update", "id-1"),
            LinkDefinition.Delete("DeleteEvent")
                .WithPermission("islamuevent_event", "delete", "id-1"),
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
                .WithPermission("islamuevent_event", "update", "id-1"),
        };

        var result = await _sut.AreLinksAllowedAsync(links, AuthenticatedUser(), _httpContext);

        await Assert.That(result[0]).IsFalse();
        await _authProvider.DidNotReceive().IsAllowedBatchAsync(
            Arg.Any<IReadOnlyList<AuthorizationCheck>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    [DisplayName("Permission-bound links without explicit action are denied without provider call")]
    public async Task PermissionResourceWithoutAction_Denied_NoProviderCall()
    {
        var links = new List<LinkDefinition>
        {
            LinkDefinition.Action("archive", "ArchiveEvent", "POST").Authenticated() with
            {
                PermissionResourceKind = "islamuevent_event",
                PermissionResourceId = "id-1",
            },
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
                .WithPermission("islamuevent_event", "update", "id-1"),
        };

        var result = await _sut.AreLinksAllowedAsync(links, null, _httpContext);

        await Assert.That(result[0]).IsFalse();
    }

    [Test]
    [DisplayName("Anonymous-advertised authenticated link is allowed without permission evaluation")]
    public async Task AdvertisedWhenAnonymous_Unauthenticated_Allowed()
    {
        var links = new List<LinkDefinition>
        {
            LinkDefinition.Action("report-event", "SubmitEventReport", "POST")
                .AdvertisedWhenAnonymous(),
        };

        var result = await _sut.AreLinksAllowedAsync(links, null, _httpContext);

        await Assert.That(result[0]).IsTrue();
        await _authProvider.DidNotReceive().IsAllowedBatchAsync(
            Arg.Any<IReadOnlyList<AuthorizationCheck>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    [DisplayName("Missing required role denies link")]
    public async Task MissingRole_Denied()
    {
        var links = new List<LinkDefinition>
        {
            LinkDefinition.Edit("UpdateEvent", roles: ["Admin"])
                .WithPermission("islamuevent_event", "update", "id-1"),
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
                .WithPermission("islamuevent_event", "update", "id-1"),
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
                .WithPermission("islamuevent_event", "update", "id-1"),                    // Condition false → false
            LinkDefinition.Delete("DeleteEvent")
                .WithPermission("islamuevent_event", "delete", "id-1"),                    // Permission check → depends on provider
            LinkDefinition.Create("CreateEvent")
                .WithPermission("islamuevent_event", "create", "islamuevent_event"),                   // Permission check → depends on provider
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

    private static bool ChecksContainScopes(
        IReadOnlyList<AuthorizationCheck> checks,
        AuthorizationScope first,
        AuthorizationScope second) =>
        checks.Count == 2 &&
        checks.Any(check => check.Scope == first) &&
        checks.Any(check => check.Scope == second);

    private static bool ChecksContainTenantAttributes(
        IReadOnlyList<AuthorizationCheck> checks,
        string firstTenantId,
        string secondTenantId) =>
        checks.Count == 2 &&
        checks.Any(check => HasTenantId(check, firstTenantId)) &&
        checks.Any(check => HasTenantId(check, secondTenantId));

    private static bool CheckContainsScopeAndAttributes(
        IReadOnlyList<AuthorizationCheck> checks,
        string tenantId,
        string organizationId) =>
        checks.Count == 1 &&
        checks[0].Scope == new AuthorizationScope(tenantId, organizationId) &&
        HasTenantId(checks[0], tenantId);

    private static bool HasTenantId(AuthorizationCheck check, string tenantId) =>
        check.ResourceAttributes is not null &&
        check.ResourceAttributes.TryGetValue("tenantId", out var value) &&
        string.Equals(value as string, tenantId, StringComparison.Ordinal);

    private static LinkDefinition RegistrationLink(IReadOnlyDictionary<string, object> attributes) =>
        LinkDefinition.Action("publish", "Publish", "POST").WithPermission(
            ResourceKinds.RegistrationForm,
            AuthorizationActions.RegistrationForms.Publish,
            "form",
            attributes);

    private static Explore.Domain.Event AuthorizationEvent(
        Guid tenantId,
        Guid eventId,
        Guid actorUserId,
        Guid organizerUserId)
    {
        var actor = new Actor
        {
            Id = Guid.CreateVersion7(),
            Pii = new ActorPii { DisplayName = "Contributor" },
            ActorTypeId = 1,
            ActorType = null!,
            UserId = actorUserId
        };
        var organizer = new Actor
        {
            Id = Guid.CreateVersion7(),
            Pii = new ActorPii { DisplayName = "Organizer" },
            ActorTypeId = 1,
            ActorType = null!,
            UserId = organizerUserId
        };
        return new Explore.Domain.Event
        {
            Id = eventId,
            TenantId = tenantId,
            Tenant = null!,
            Title = "Authorization target",
            ActorId = actor.Id,
            Actor = actor,
            OrganizerActorId = organizer.Id,
            OrganizerActor = organizer,
            EventStatus = null!,
            EventFormat = null!,
            VisibilityType = null!
        };
    }

    private sealed record TestResource(string Id, string TenantId, string OrganizationId);
}
