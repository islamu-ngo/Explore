// ABOUTME: Unit tests for normalized webhook audit principal resolution and fail-closed persistence.
// ABOUTME: Proves user, machine, and explicit system identities cannot silently degrade to anonymous audit.

using Explore.Application.Authentication;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Services.Webhooks;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Webhooks;

public sealed class WebhookAuditEventWriterTests
{
    [Test]
    public async Task AppendAsync_WhenUserIsAuthenticated_PersistsNormalizedUserPrincipal()
    {
        var actorUserId = Guid.CreateVersion7();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(actorUserId);
        currentUser.IsAuthenticated.Returns(true);
        var fixture = CreateFixture(currentUser: currentUser);
        var request = CreateRequest();

        var result = await fixture.Writer.AppendAsync(request, CancellationToken.None);

        await Assert.That(result.PrincipalKind).IsEqualTo(WebhookAuditPrincipalKind.User);
        await Assert.That(result.PrincipalReference).IsEqualTo($"user:{actorUserId:D}");
        await Assert.That(result.EffectiveScopeId).IsEqualTo(request.TenantId);
        await fixture.Repository.Received(1).AppendAsync(result, CancellationToken.None);
    }

    [Test]
    public async Task AppendAsync_WhenMachineIsAuthenticated_PersistsNormalizedOwnerPrincipal()
    {
        var ownerId = Guid.CreateVersion7();
        var machinePrincipal = Substitute.For<IMachinePrincipalAccessor>();
        machinePrincipal.Current.Returns(new ApiKeyPrincipalContext(
            KeyId: "key-audit-worker",
            TenantId: null,
            OwnerType: ExternalApiKeyOwnerType.InstanceAdmin,
            OwnerId: ownerId,
            Scopes: []));
        machinePrincipal.IsMachineCaller.Returns(true);
        var fixture = CreateFixture(machinePrincipal: machinePrincipal);

        var result = await fixture.Writer.AppendAsync(CreateRequest(), CancellationToken.None);

        await Assert.That(result.PrincipalKind).IsEqualTo(WebhookAuditPrincipalKind.Machine);
        await Assert.That(result.PrincipalReference)
            .IsEqualTo($"machine:{ExternalApiKeyOwnerType.InstanceAdmin}:{ownerId:D}");
    }

    [Test]
    public async Task AppendAsync_WithExplicitSystemPrincipal_OverridesAmbientIdentity()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.CreateVersion7());
        currentUser.IsAuthenticated.Returns(true);
        var fixture = CreateFixture(currentUser: currentUser);
        var request = CreateRequest() with
        {
            PrincipalKind = WebhookAuditPrincipalKind.System,
            PrincipalReference = "system:webhook-delivery-worker"
        };

        var result = await fixture.Writer.AppendAsync(request, CancellationToken.None);

        await Assert.That(result.PrincipalKind).IsEqualTo(WebhookAuditPrincipalKind.System);
        await Assert.That(result.PrincipalReference).IsEqualTo("system:webhook-delivery-worker");
    }

    [Test]
    public async Task AppendAsync_WithoutAuthenticatedOrExplicitPrincipal_FailsBeforePersistence()
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Writer.AppendAsync(CreateRequest(), CancellationToken.None));

        await Assert.That(exception.Message).Contains("principal is required");
        await fixture.Repository.DidNotReceive().AppendAsync(
            Arg.Any<WebhookAuditEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AppendAsync_WithExplicitPrincipalMissingReference_FailsBeforePersistence()
    {
        var fixture = CreateFixture();
        var request = CreateRequest() with
        {
            PrincipalKind = WebhookAuditPrincipalKind.System,
            PrincipalReference = " "
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Writer.AppendAsync(request, CancellationToken.None));

        await Assert.That(exception.Message).Contains("requires a reference");
        await fixture.Repository.DidNotReceive().AppendAsync(
            Arg.Any<WebhookAuditEvent>(),
            Arg.Any<CancellationToken>());
    }

    private static WebhookAuditWriteRequest CreateRequest()
    {
        var tenantId = Guid.CreateVersion7();
        return new WebhookAuditWriteRequest(
            tenantId,
            WebhookAuditAction.EndpointCreated,
            WebhookAuditTargetKind.Endpoint,
            Guid.CreateVersion7(),
            "endpoint_created",
            WebhookAuditOutcome.Succeeded);
    }

    private static Fixture CreateFixture(
        ICurrentUserService? currentUser = null,
        IMachinePrincipalAccessor? machinePrincipal = null)
    {
        var repository = Substitute.For<IWebhookAuditEventRepository>();
        repository.AppendAsync(Arg.Any<WebhookAuditEvent>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<WebhookAuditEvent>());
        var retentionPolicyResolver = Substitute.For<IWebhookRetentionPolicyResolver>();
        retentionPolicyResolver.Resolve(
                Arg.Any<DateTimeOffset>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<int?>())
            .Returns(call => CreateRetentionPolicy(call.ArgAt<DateTimeOffset>(1)));
        var writer = new WebhookAuditEventWriter(
            repository,
            currentUser ?? Substitute.For<ICurrentUserService>(),
            machinePrincipal ?? Substitute.For<IMachinePrincipalAccessor>(),
            retentionPolicyResolver,
            TimeProvider.System);
        return new Fixture(repository, writer);
    }

    private static WebhookRetentionPolicySnapshot CreateRetentionPolicy(DateTimeOffset materializedAt) =>
        new(
            "webhook-retention-test-v1",
            materializedAt.AddDays(14),
            materializedAt.AddDays(14),
            materializedAt.AddDays(30),
            materializedAt.AddDays(90),
            materializedAt.AddDays(90),
            materializedAt.AddDays(30),
            materializedAt.AddDays(365),
            materializedAt.AddDays(14));

    private sealed record Fixture(
        IWebhookAuditEventRepository Repository,
        WebhookAuditEventWriter Writer);
}
