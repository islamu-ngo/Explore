// ABOUTME: Architecture guards for coordinated system and tenant setting mutation contracts.
// ABOUTME: Prevents generic CRUD APIs from bypassing per-key locks and safe repository operations.

using System.Reflection;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.ControlPlane.Handlers.Commands;
using Explore.Application.Notifications;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Persistence;
using Explore.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Event.Architecture.Tests;

public sealed class SettingMutationBoundaryArchitectureTests
{
    [Test]
    public async Task SettingMutationLock_ShouldExposeMultiKeyExecution()
    {
        MethodInfo? method = typeof(ISettingMutationLock).GetMethod("ExecuteManyAsync");

        await Assert.That(method).IsNotNull()
            .Because("tenant policy batches must acquire all canonical setting locks in deterministic order");
    }

    [Test]
    public async Task SettingRepositoryContracts_ShouldNotInheritGenericCrud()
    {
        Type genericRepository = typeof(IGenericRepository<,>);
        Type[] violations =
        [
            .. typeof(ITenantSettingRepository).GetInterfaces()
                .Where(candidate => candidate.IsGenericType
                    && candidate.GetGenericTypeDefinition() == genericRepository),
            .. typeof(ISystemSettingRepository).GetInterfaces()
                .Where(candidate => candidate.IsGenericType
                    && candidate.GetGenericTypeDefinition() == genericRepository)
        ];

        await Assert.That(violations).IsEmpty()
            .Because("setting mutations must use explicit coordinated repository operations");
    }

    [Test]
    public async Task SettingRepositoryImplementations_ShouldNotExposeGenericMutationMethods()
    {
        string[] forbiddenNames = ["Create", "Update", "Delete"];
        string[] violations = new[] { typeof(TenantSettingRepository), typeof(SystemSettingRepository) }
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(method => forbiddenNames.Contains(method.Name, StringComparer.Ordinal))
                .Select(method => $"{type.FullName}.{method.Name}"))
            .ToArray();

        await Assert.That(violations).IsEmpty()
            .Because("concrete setting repositories must not retain inherited mutation escape hatches");
    }

    [Test]
    public async Task TenantPolicyContract_ShouldReturnNotificationsForPostCommitPublication()
    {
        MethodInfo method = typeof(ITenantPolicySettingService)
            .GetMethod(nameof(ITenantPolicySettingService.ApplyTenantSettingsAsync))!;

        await Assert.That(method.ReturnType).IsEqualTo(typeof(Task<IReadOnlyList<SettingChangedNotification>>));
        await Assert.That(method.GetParameters().Last().ParameterType).IsEqualTo(typeof(CancellationToken));
    }

    [Test]
    public async Task DirectControlPlaneSettingHandlers_ShouldRequireTrustedCurrentUserContext()
    {
        Type[] handlerTypes =
        [
            typeof(SetControlPlaneTenantSettingCommandHandler),
            typeof(LockControlPlaneTenantSettingCommandHandler),
            typeof(UnlockControlPlaneTenantSettingCommandHandler)
        ];

        string[] violations = handlerTypes
            .Where(type => !type.GetConstructors().Single().GetParameters()
                .Any(parameter => parameter.ParameterType == typeof(ICurrentUserService)))
            .Select(type => type.Name)
            .ToArray();

        await Assert.That(violations).IsEmpty();
    }

    [Test]
    public async Task ControlPlaneMutationHandlers_ShouldDeclarePostCommitSideEffectDependencies()
    {
        Type[] handlerTypes =
        [
            typeof(SetControlPlaneTenantSettingCommandHandler),
            typeof(LockControlPlaneTenantSettingCommandHandler),
            typeof(UnlockControlPlaneTenantSettingCommandHandler),
            typeof(ApplyControlPlaneTenantPlanAssignmentCommandHandler)
        ];

        string[] violations = handlerTypes
            .Where(type => !type.GetConstructors().Single().GetParameters()
                .Any(parameter => parameter.ParameterType == typeof(MediatR.IMediator)))
            .Select(type => type.Name)
            .ToArray();

        await Assert.That(violations).IsEmpty();
    }

    [Test]
    public async Task PersistenceRegistration_ShouldNotExposeGenericSettingRepositories()
    {
        var services = new ServiceCollection();
        services.ConfigurePersistenceServices(
            new ConfigurationBuilder().Build(),
            skipDbContextRegistration: true,
            skipLookupCacheInitializer: true);

        bool hasOpenGeneric = services.Any(descriptor =>
            descriptor.ServiceType == typeof(IGenericRepository<,>));
        bool hasSystemSetting = services.Any(descriptor =>
            descriptor.ServiceType == typeof(IGenericRepository<SystemSetting, Guid>));
        bool hasTenantSetting = services.Any(descriptor =>
            descriptor.ServiceType == typeof(IGenericRepository<TenantSetting, Guid>));
        bool hasLegitimateClosedRepository = services.Any(descriptor =>
            descriptor.ServiceType == typeof(IGenericRepository<EventReportDecision, Guid>));

        using ServiceProvider provider = services.BuildServiceProvider();
        object? resolvedSystemSettingRepository = provider.GetService<IGenericRepository<SystemSetting, Guid>>();
        object? resolvedTenantSettingRepository = provider.GetService<IGenericRepository<TenantSetting, Guid>>();

        await Assert.That(hasOpenGeneric).IsFalse();
        await Assert.That(hasSystemSetting).IsFalse();
        await Assert.That(hasTenantSetting).IsFalse();
        await Assert.That(hasLegitimateClosedRepository).IsTrue();
        await Assert.That(resolvedSystemSettingRepository).IsNull();
        await Assert.That(resolvedTenantSettingRepository).IsNull();
    }
}
