// ABOUTME: Tests authorized provider-binding creation, rebind, ownership rejection, and audit evidence.
// ABOUTME: Proves remote verification precedes an optimistic same-transaction binding mutation.

using System.Reflection;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Features.Webhooks.Handlers.Commands;
using Explore.Application.Features.Webhooks.Requests.Commands;
using Explore.Domain;
using NSubstitute;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Webhooks;

public sealed class RepairWebhookProviderBindingCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task MissingCurrentBinding_VerifiesExpectedUidThenCreatesVerifiedBindingAndAudit()
    {
        var fixture = new Fixture();
        var request = fixture.CreateRequest("app_new_consumer");

        var response = await fixture.Handler.Handle(request, CancellationToken.None);

        await Assert.That(response.Success).IsTrue();
        var created = fixture.CreatedBinding;
        await Assert.That(created).IsNotNull();
        await Assert.That(created!.InstanceId).IsEqualTo(fixture.Bootstrap.Id);
        await Assert.That(created.WebhookConsumerId).IsEqualTo(fixture.Consumer.Id);
        await Assert.That(created.IsVerifiedFor(fixture.TenantId, fixture.Consumer.Id)).IsTrue();
        await fixture.Authority.Received(1).VerifyOwnershipAsync(
            Arg.Is<WebhookProviderBindingOwnershipRequest>(proof =>
                proof.Ownership.TenantId == fixture.TenantId &&
                proof.WebhookConsumerId == fixture.Consumer.Id &&
                proof.ApplicationUid == WebhookConsumerProviderBinding.CreateApplicationUid(
                    fixture.Bootstrap.Id,
                    fixture.Consumer.Id) &&
                proof.ExternalApplicationId == request.ExternalApplicationId),
            Arg.Any<CancellationToken>());
        await fixture.AuditWriter.Received(1).AppendAsync(
            Arg.Is<WebhookAuditWriteRequest>(audit =>
                audit.Action == WebhookAuditAction.ProviderBindingRepairSucceeded &&
                audit.ReasonCode == request.ReasonCode &&
                audit.SafeAfterJson != null &&
                !audit.SafeAfterJson.Contains(request.ExternalApplicationId, StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExistingVerifiedBinding_RebindsWithCanonicalInstanceIdentityAndAdvancesFence()
    {
        var fixture = new Fixture();
        var oldInstanceId = Guid.CreateVersion7();
        var binding = WebhookConsumerProviderBinding.CreatePending(
            fixture.TenantId,
            fixture.Consumer.Id,
            oldInstanceId,
            fixture.Profile.ProviderEnvironment,
            fixture.Profile.CapabilityProfile,
            fixture.Profile.GovernanceAllowedCapabilities);
        binding.VerifyOwnership(
            fixture.TenantId,
            fixture.Consumer.Id,
            "app_old",
            Now.AddMinutes(-5));
        fixture.UseExistingBinding(binding);
        var request = fixture.CreateRequest("app_rebound");

        var response = await fixture.Handler.Handle(request, CancellationToken.None);

        await Assert.That(response.Success).IsTrue();
        await Assert.That(binding.ExternalApplicationId).IsEqualTo("app_rebound");
        await Assert.That(binding.InstanceId).IsEqualTo(fixture.Bootstrap.Id);
        await Assert.That(binding.ConcurrencyVersion).IsEqualTo(3);
        await Assert.That(binding.VerificationFence).IsEqualTo(3);
        await fixture.BindingRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await fixture.BindingRepository.DidNotReceive().CreateAsync(
            Arg.Any<WebhookConsumerProviderBinding>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RemoteOwnershipMismatch_WritesRejectedAuditWithoutMutatingBinding()
    {
        var fixture = new Fixture();
        fixture.Authority.VerifyOwnershipAsync(
                Arg.Any<WebhookProviderBindingOwnershipRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(WebhookProviderBindingOwnershipResult.Failure(
                "webhook_provider_binding_mismatched",
                isRetryable: false));
        var request = fixture.CreateRequest("app_wrong_owner");

        var response = await fixture.Handler.Handle(request, CancellationToken.None);

        await Assert.That(response.Success).IsFalse();
        await Assert.That(response.FailureCode).IsEqualTo("webhook_provider_binding_mismatched");
        await fixture.BindingRepository.DidNotReceive().CreateAsync(
            Arg.Any<WebhookConsumerProviderBinding>(),
            Arg.Any<CancellationToken>());
        await fixture.BindingRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await fixture.AuditWriter.Received(1).AppendAsync(
            Arg.Is<WebhookAuditWriteRequest>(audit =>
                audit.Action == WebhookAuditAction.ProviderBindingRepairRejected &&
                audit.Outcome == WebhookAuditOutcome.Rejected &&
                audit.SafeAfterJson != null &&
                !audit.SafeAfterJson.Contains(request.ExternalApplicationId, StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task WrongTenantConsumer_FailsBeforeProfileResolutionOrProviderCall()
    {
        var fixture = new Fixture();
        fixture.ConsumerRepository.GetByIdForOwnerOperationAsync(
                fixture.Consumer.Id,
                false,
                Arg.Any<CancellationToken>())
            .Returns((WebhookConsumer?)null);

        var response = await fixture.Handler.Handle(
            fixture.CreateRequest("app_cross_tenant"),
            CancellationToken.None);

        await Assert.That(response.Success).IsFalse();
        await Assert.That(response.FailureCode).IsEqualTo("webhook_consumer_not_found");
        fixture.Authority.DidNotReceive().ResolveCurrentProfile();
        await fixture.Authority.DidNotReceive().VerifyOwnershipAsync(
            Arg.Any<WebhookProviderBindingOwnershipRequest>(),
            Arg.Any<CancellationToken>());
        await fixture.AuditWriter.DidNotReceive().AppendAsync(
            Arg.Any<WebhookAuditWriteRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CommandAuthorization_UsesManageProviderAndConsumerResourceIdentity()
    {
        var consumerId = Guid.CreateVersion7();
        var attribute = typeof(RepairWebhookProviderBindingCommand)
            .GetCustomAttribute<AuthorizeResourceAttribute>();
        ISecureRequest request = new RepairWebhookProviderBindingCommand
        {
            ConsumerId = consumerId,
            ExternalApplicationId = "app_authorized",
            ReasonCode = "provider.application-recreated"
        };

        await Assert.That(attribute).IsNotNull();
        await Assert.That(attribute!.Resource).IsEqualTo(ResourceKinds.Webhook);
        await Assert.That(attribute.Action).IsEqualTo(AuthorizationActions.Webhooks.ManageProvider);
        await Assert.That(request.ResourceId).IsEqualTo(consumerId.ToString("D"));
        // Ownership is resolved server-side from the persisted consumer, not declared by the request.
        await Assert.That(request.AuthorizationFacts).IsNull();
    }

    private sealed class Fixture
    {
        public Fixture()
        {
            TenantId = Guid.CreateVersion7();
            ActorUserId = Guid.CreateVersion7();
            Bootstrap = new InstanceBootstrapState
            {
                Id = Guid.CreateVersion7(),
                IsCompleted = true,
                CreatedAt = Now.UtcDateTime,
                CompletedAt = Now.UtcDateTime,
                CompletedByUserId = ActorUserId
            };
            Consumer = new WebhookConsumer
            {
                Id = Guid.CreateVersion7(),
                TenantId = TenantId,
                ConsumerKind = WebhookConsumerKind.Tenant,
                Name = "Provider binding repair consumer",
                Status = WebhookConsumerStatus.Active,
                ProviderMode = WebhookProviderMode.Svix
            };
            var capabilityProfile = WebhookProviderCapabilityProfile.Create(
                WebhookProviderKind.Svix,
                "1.96.1",
                WebhookProviderCapability.AppPortal,
                "svix-self-hosted-1.96.1-v1",
                Now);
            Profile = new WebhookProviderBindingProfile(
                WebhookProviderKind.Svix,
                "self-hosted",
                capabilityProfile,
                WebhookProviderCapability.AppPortal);

            ConsumerRepository = Substitute.For<IWebhookConsumerRepository>();
            ConsumerRepository.GetByIdForOwnerOperationAsync(
                    Consumer.Id,
                    false,
                    Arg.Any<CancellationToken>())
                .Returns(Consumer);
            BindingRepository = Substitute.For<IWebhookConsumerProviderBindingRepository>();
            BindingRepository.CreateAsync(
                    Arg.Any<WebhookConsumerProviderBinding>(),
                    Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    CreatedBinding = call.Arg<WebhookConsumerProviderBinding>();
                    return CreatedBinding;
                });
            BindingRepository.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            BootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
            BootstrapRepository.GetCurrent(Arg.Any<CancellationToken>()).Returns(Bootstrap);
            Authority = Substitute.For<IWebhookProviderBindingAuthorityService>();
            Authority.ResolveCurrentProfile().Returns(WebhookProviderBindingProfileResult.Success(Profile));
            Authority.VerifyOwnershipAsync(
                    Arg.Any<WebhookProviderBindingOwnershipRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(WebhookProviderBindingOwnershipResult.Success());
            AuditWriter = Substitute.For<IWebhookAuditEventWriter>();
            var currentUser = Substitute.For<ICurrentUserService>();
            currentUser.UserId.Returns(ActorUserId);

            Handler = new RepairWebhookProviderBindingCommandHandler(
                ConsumerRepository,
                BindingRepository,
                BootstrapRepository,
                Authority,
                AuditWriter,
                new InlineUnitOfWork(),
                currentUser,
                Substitute.For<IMachinePrincipalAccessor>(),
                new FixedTimeProvider(Now));
        }

        public Guid TenantId { get; }
        public Guid ActorUserId { get; }
        public InstanceBootstrapState Bootstrap { get; }
        public WebhookConsumer Consumer { get; }
        public WebhookProviderBindingProfile Profile { get; }
        public IWebhookConsumerRepository ConsumerRepository { get; }
        public IWebhookConsumerProviderBindingRepository BindingRepository { get; }
        public IInstanceBootstrapStateRepository BootstrapRepository { get; }
        public IWebhookProviderBindingAuthorityService Authority { get; }
        public IWebhookAuditEventWriter AuditWriter { get; }
        public RepairWebhookProviderBindingCommandHandler Handler { get; }
        public WebhookConsumerProviderBinding? CreatedBinding { get; private set; }

        public RepairWebhookProviderBindingCommand CreateRequest(string externalApplicationId) => new()
        {
            ConsumerId = Consumer.Id,
            ExternalApplicationId = externalApplicationId,
            ReasonCode = "provider.application-recreated"
        };

        public void UseExistingBinding(WebhookConsumerProviderBinding binding)
        {
            BindingRepository.GetByConsumerAsync(
                    TenantId,
                    Consumer.Id,
                    Profile.ProviderKind,
                    Profile.ProviderEnvironment,
                    Arg.Any<CancellationToken>())
                .Returns(binding);
            BindingRepository.GetByTenantAndIdForUpdateAsync(
                    TenantId,
                    binding.Id,
                    Arg.Any<CancellationToken>())
                .Returns(binding);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class InlineUnitOfWork : IUnitOfWork
    {
        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken ct = default) =>
            await operation(ct);

        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default) =>
            operation(ct);

        public Task<T> ExecuteSerializableAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default) => ExecuteInTransactionAsync(operation, ct);
    }
}
