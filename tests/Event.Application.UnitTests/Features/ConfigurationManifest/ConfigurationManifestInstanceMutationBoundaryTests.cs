// ABOUTME: Verifies typed caller-owned transaction seams for instance manifest settings.
// ABOUTME: Proves scalar writes defer effects and guarded or sensitive keys fail before persistence.

namespace Event.Application.UnitTests.Features.ConfigurationManifest;

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.ConfigurationManifest.Application;
using Explore.Application.Features.ConfigurationManifest.Application;
using Explore.Application.Features.ConfigurationManifest.Handlers.Commands;
using Explore.Application.Notifications;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Settings;
using Explore.Domain.Settings.Definitions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

public sealed class ConfigurationManifestInstanceMutationBoundaryTests
{
    private static readonly Guid Actor =
        Guid.Parse("0199464e-e388-7f56-9281-cefabd6a5674");

    private static readonly DateTime OccurredAtUtc =
        new(2026, 8, 26, 10, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task ManifestHandler_HasNoDirectSettingRepositoryDependency()
    {
        Type[] constructorDependencies =
            typeof(ApplyConfigurationManifestCommandHandler)
                .GetConstructors()
                .Single()
                .GetParameters()
                .Select(parameter => parameter.ParameterType)
                .ToArray();

        await Assert.That(constructorDependencies.Contains(
            typeof(ISystemSettingRepository))).IsFalse();
        await Assert.That(constructorDependencies.Contains(
            typeof(ITenantSettingRepository))).IsFalse();
    }

    [Test]
    public async Task ScalarMutation_PersistsCanonicalMetadataAndDefersEffects()
    {
        ISystemSettingRepository repository =
            Substitute.For<ISystemSettingRepository>();
        IMediator mediator = Substitute.For<IMediator>();
        IPublicationPolicyMutationBoundary publication =
            Substitute.For<IPublicationPolicyMutationBoundary>();
        SystemSetting? persisted = null;
        repository.UpsertInCurrentTransactionAsync(
                Arg.Do<SystemSetting>(setting => persisted = setting),
                Arg.Any<CancellationToken>())
            .Returns("\"Old label\"");
        var service = new SettingUpsertService(
            repository,
            mediator,
            publication);

        InstanceSettingMutationResult result =
            await service.UpsertInstanceValueInCurrentTransactionAsync(
                new InstanceSettingMutationInput(
                    PublicExperienceSettingDefinitions.EventCatalogLabel.Key,
                    "\"Community Events\"",
                    Actor,
                    OccurredAtUtc));

        await Assert.That(persisted).IsNotNull();
        if (persisted is null)
            return;

        await Assert.That(persisted.SettingKey)
            .IsEqualTo(
                PublicExperienceSettingDefinitions.EventCatalogLabel.Key);
        await Assert.That(persisted.Value).IsEqualTo("\"Community Events\"");
        await Assert.That(persisted.ValueType)
            .IsEqualTo(
                PublicExperienceSettingDefinitions.EventCatalogLabel.ValueType);
        await Assert.That(persisted.CreatedAt).IsEqualTo(OccurredAtUtc);
        await Assert.That(persisted.UpdatedAt).IsEqualTo(OccurredAtUtc);
        await Assert.That(persisted.CreatedBy).IsEqualTo(Actor);
        await Assert.That(persisted.UpdatedBy).IsEqualTo(Actor);
        await Assert.That(persisted.IsLocked).IsFalse();
        await Assert.That(result.Notification.Key)
            .IsEqualTo(
                PublicExperienceSettingDefinitions.EventCatalogLabel.Key);
        await Assert.That(result.Notification.OldValue)
            .IsEqualTo("\"Old label\"");
        await Assert.That(result.Notification.NewValue)
            .IsEqualTo("\"Community Events\"");
        await Assert.That(result.Notification.ChangedAt)
            .IsEqualTo(OccurredAtUtc);
        await mediator.DidNotReceive().Publish(
            Arg.Any<SettingChangedNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GuardedMutation_RejectsBeforeGenericRepositoryWrite()
    {
        ISystemSettingRepository repository =
            Substitute.For<ISystemSettingRepository>();
        var service = new SettingUpsertService(
            repository,
            Substitute.For<IMediator>(),
            Substitute.For<IPublicationPolicyMutationBoundary>());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpsertInstanceValueInCurrentTransactionAsync(
                new InstanceSettingMutationInput(
                    EventSettingDefinitions.RequireApproval.Key,
                    "true",
                    Actor,
                    OccurredAtUtc)));

        await repository.DidNotReceive().UpsertInCurrentTransactionAsync(
            Arg.Any<SystemSetting>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SensitiveMutation_RejectsBeforeGenericRepositoryWrite()
    {
        ISystemSettingRepository repository =
            Substitute.For<ISystemSettingRepository>();
        var service = new SettingUpsertService(
            repository,
            Substitute.For<IMediator>(),
            Substitute.For<IPublicationPolicyMutationBoundary>());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpsertInstanceValueInCurrentTransactionAsync(
                new InstanceSettingMutationInput(
                    "analytics.personal_api_key",
                    "\"not-persisted\"",
                    Actor,
                    OccurredAtUtc)));

        await repository.DidNotReceive().UpsertInCurrentTransactionAsync(
            Arg.Any<SystemSetting>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TenantScalarBoundary_WritesOnlyCatalogOwnedUnguardedKeys()
    {
        ITenantSettingRepository repository =
            Substitute.For<ITenantSettingRepository>();
        var boundary =
            new ConfigurationManifestTenantSettingMutationBoundary(repository);
        Guid tenantId = Guid.CreateVersion7();

        await boundary.CreateInCurrentTransactionAsync(
            new ConfigurationManifestTenantSettingMutationInput(
                tenantId,
                [
                    new ConfigurationManifestTenantSettingMutation(
                        PublicExperienceSettingDefinitions.EventCatalogLabel.Key,
                        "\"Community Events\"")
                ],
                Actor,
                OccurredAtUtc));

        await repository.Received(1).CreateManyForTenantAsync(
            tenantId,
            Arg.Is<IReadOnlyCollection<TenantSettingOverrideUpsert>>(writes =>
                writes.Count == 1
                && writes.Single().SettingKey
                    == PublicExperienceSettingDefinitions.EventCatalogLabel.Key
                && writes.Single().Value == "\"Community Events\""
                && !writes.Single().IsLocked),
            Actor,
            OccurredAtUtc,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TenantScalarBoundary_RejectsGuardedKeyBeforeRepositoryWrite()
    {
        ITenantSettingRepository repository =
            Substitute.For<ITenantSettingRepository>();
        var boundary =
            new ConfigurationManifestTenantSettingMutationBoundary(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            boundary.CreateInCurrentTransactionAsync(
                new ConfigurationManifestTenantSettingMutationInput(
                    Guid.CreateVersion7(),
                    [
                        new ConfigurationManifestTenantSettingMutation(
                            EventSettingDefinitions.RequireApproval.Key,
                            "true")
                    ],
                    Actor,
                    OccurredAtUtc)));

        await repository.DidNotReceiveWithAnyArgs()
            .CreateManyForTenantAsync(default, default!, default, default, default);
    }

    [Test]
    public async Task InstanceDispatcher_RoutesGuardedFirstAndDefersAllEffects()
    {
        ISystemSettingRepository repository =
            Substitute.For<ISystemSettingRepository>();
        IMediator mediator = Substitute.For<IMediator>();
        IPublicationPolicyMutationBoundary publication =
            Substitute.For<IPublicationPolicyMutationBoundary>();
        publication.ApplyInstanceInCurrentTransactionAsync(
                Arg.Any<PublicationPolicyInstanceMutationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new PublicationPolicyMutationResult(
                Success: true,
                FailureCode: null,
                Message: "updated",
                [
                    new SettingChangedNotification(
                        EventSettingDefinitions.RequireApproval.Key,
                        "false",
                        "true",
                        SettingSource.SystemDefault,
                        null,
                        Actor,
                        OccurredAtUtc)
                ]));
        var dispatcher =
            new ConfigurationManifestInstanceSettingMutationBoundary(
                new SettingUpsertService(repository, mediator, publication),
                publication);

        ConfigurationManifestInstanceSettingMutationResult result =
            await dispatcher.ApplyInCurrentTransactionAsync(
                new ConfigurationManifestInstanceSettingMutationInput(
                    [
                        new ConfigurationManifestInstanceSettingMutation(
                            PublicExperienceSettingDefinitions.EventCatalogLabel.Key,
                            "\"Community Events\""),
                        new ConfigurationManifestInstanceSettingMutation(
                            EventSettingDefinitions.RequireApproval.Key,
                            "true")
                    ],
                    Actor,
                    OccurredAtUtc));

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.DeferredNotifications.Length).IsEqualTo(2);
        await publication.Received(1).ApplyInstanceInCurrentTransactionAsync(
            Arg.Is<PublicationPolicyInstanceMutationRequest>(request =>
                request.ActorUserId == Actor
                && request.Mutations.Length == 1
                && request.Mutations[0].Key
                    == EventSettingDefinitions.RequireApproval.Key),
            Arg.Any<CancellationToken>());
        await publication.DidNotReceiveWithAnyArgs()
            .ApplyInstanceAsync(default!, default);
        await repository.Received(1).UpsertInCurrentTransactionAsync(
            Arg.Is<SystemSetting>(setting =>
                setting.SettingKey
                    == PublicExperienceSettingDefinitions.EventCatalogLabel.Key),
            Arg.Any<CancellationToken>());
        await mediator.DidNotReceive().Publish(
            Arg.Any<SettingChangedNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task InstanceDispatcher_GuardedRejectionPrecedesScalarWrite()
    {
        ISystemSettingRepository repository =
            Substitute.For<ISystemSettingRepository>();
        IPublicationPolicyMutationBoundary publication =
            Substitute.For<IPublicationPolicyMutationBoundary>();
        publication.ApplyInstanceInCurrentTransactionAsync(
                Arg.Any<PublicationPolicyInstanceMutationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new PublicationPolicyMutationResult(
                Success: false,
                FailureCode: "unsafe",
                Message: "rejected",
                DeferredNotifications: []));
        var dispatcher =
            new ConfigurationManifestInstanceSettingMutationBoundary(
                new SettingUpsertService(
                    repository,
                    Substitute.For<IMediator>(),
                    publication),
                publication);

        ConfigurationManifestInstanceSettingMutationResult result =
            await dispatcher.ApplyInCurrentTransactionAsync(
                new ConfigurationManifestInstanceSettingMutationInput(
                    [
                        new ConfigurationManifestInstanceSettingMutation(
                            PublicExperienceSettingDefinitions.EventCatalogLabel.Key,
                            "\"Community Events\""),
                        new ConfigurationManifestInstanceSettingMutation(
                            EventSettingDefinitions.RequireApproval.Key,
                            "true")
                    ],
                    Actor,
                    OccurredAtUtc));

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("unsafe");
        await repository.DidNotReceive().UpsertInCurrentTransactionAsync(
            Arg.Any<SystemSetting>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ManifestRegistration_RegistersBothTypedSettingBoundaries()
    {
        var services = new ServiceCollection();

        services.AddConfigurationManifestApplication();

        await Assert.That(services.Any(descriptor =>
            descriptor.ServiceType
                == typeof(IConfigurationManifestInstanceSettingMutationBoundary)
            && descriptor.ImplementationType
                == typeof(ConfigurationManifestInstanceSettingMutationBoundary)))
            .IsTrue();
        await Assert.That(services.Any(descriptor =>
            descriptor.ServiceType
                == typeof(IConfigurationManifestTenantSettingMutationBoundary)
            && descriptor.ImplementationType
                == typeof(ConfigurationManifestTenantSettingMutationBoundary)))
            .IsTrue();
    }
}
