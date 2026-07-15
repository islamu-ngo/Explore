// ABOUTME: Focused tests for default-off Event managed mode and directional credential lifecycle invariants.
// ABOUTME: Proves standalone short-circuiting plus registered-only rotation and irreversible revocation.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Management;
using Explore.Application.Features.Management;
using Explore.Application.Features.Management.Handlers.Queries;
using Explore.Application.Features.Management.Requests.Queries;
using Explore.Application.Management;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Management;

public sealed class ManagedControlPlaneRegistrationTests
{
    [Test]
    public async Task Capabilities_WhenManagedModeIsDisabled_ReturnsAbsentWithoutRuntimeReads()
    {
        var deploymentModeProvider = Substitute.For<IDeploymentModeProvider>();
        var bootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
        var registrationRepository = Substitute.For<IManagedControlPlaneRegistrationRepository>();
        var options = Microsoft.Extensions.Options.Options.Create(new ManagedControlPlaneOptions());
        var capacityReader = new ManagedTenantProvisioningCapacityReader(new TenantActivationCapacityPolicy(
            bootstrapRepository,
            Substitute.For<ITenantRepository>(),
            Substitute.For<IManagedTenantProvisioningOperationRepository>(),
            options));
        var handler = new GetManagementCapabilitiesQueryHandler(
            options,
            bootstrapRepository,
            registrationRepository,
            capacityReader);

        var result = await handler.Handle(new GetManagementCapabilitiesQuery(), CancellationToken.None);

        await Assert.That(result.ManagedModeEnabled).IsFalse();
        await Assert.That(result.Capabilities).IsEmpty();
        await deploymentModeProvider.DidNotReceive().GetCurrentModeAsync(Arg.Any<CancellationToken>());
        await bootstrapRepository.DidNotReceive().GetCurrent(Arg.Any<CancellationToken>());
        await registrationRepository.DidNotReceive().GetCurrentAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments(DeploymentMode.SingleTenant, 10)]
    [Arguments(DeploymentMode.MultiTenant, 0)]
    public async Task Capabilities_WhenTenantProvisioningUnavailable_DoesNotReadOrExposeCapacity(
        DeploymentMode mode,
        int maximumTenantCount)
    {
        var deploymentModeProvider = Substitute.For<IDeploymentModeProvider>();
        deploymentModeProvider.GetCurrentModeAsync(Arg.Any<CancellationToken>()).Returns(mode);
        var tenantRepository = Substitute.For<ITenantRepository>();
        var operationRepository = Substitute.For<IManagedTenantProvisioningOperationRepository>();
        var options = Microsoft.Extensions.Options.Options.Create(new ManagedControlPlaneOptions
        {
            Enabled = true,
            MaximumTenantCount = maximumTenantCount
        });
        var bootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
        bootstrapRepository.GetCurrent(Arg.Any<CancellationToken>()).Returns(new InstanceBootstrapState
        {
            Id = Guid.CreateVersion7(),
            IsCompleted = true,
            SelectedDeploymentMode = mode.ToString(),
            CreatedAt = DateTime.UtcNow
        });
        var capacityReader = new ManagedTenantProvisioningCapacityReader(new TenantActivationCapacityPolicy(
            bootstrapRepository,
            tenantRepository,
            operationRepository,
            options));
        var handler = new GetManagementCapabilitiesQueryHandler(
            options,
            bootstrapRepository,
            Substitute.For<IManagedControlPlaneRegistrationRepository>(),
            capacityReader);

        ManagementCapabilitiesDto result = await handler.Handle(
            new GetManagementCapabilitiesQuery(),
            CancellationToken.None);

        await Assert.That(result.TenantProvisioningCapacity).IsNull();
        await Assert.That(result.Capabilities
                .Intersect(ManagedControlPlaneContract.TenantProvisioningCapabilities))
            .IsEmpty();
        await deploymentModeProvider.DidNotReceive().GetCurrentModeAsync(Arg.Any<CancellationToken>());
        await tenantRepository.DidNotReceive().GetActiveTenantCountAsync();
        await operationRepository.DidNotReceive().CountActiveReservationsAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Registration_AfterRegistration_RotatesThenRevokesIrreversibly()
    {
        var createdAt = DateTime.SpecifyKind(new DateTime(2026, 7, 11, 20, 0, 0), DateTimeKind.Utc);
        var registration = NewRegistration(createdAt);

        registration.MarkRegistered(createdAt.AddMinutes(1));
        registration.RotateControlPlaneCredential(
            "replacement-key",
            Convert.ToBase64String(new byte[32]),
            createdAt.AddDays(30),
            createdAt.AddMinutes(2));
        registration.Revoke(createdAt.AddMinutes(3));

        await Assert.That(registration.Status).IsEqualTo(ManagedControlPlaneRegistrationStatus.Revoked);
        await Assert.That(registration.ControlPlaneToEventKeyId).IsEqualTo("replacement-key");
        await Assert.That(registration.RegisteredAt).IsEqualTo(createdAt.AddMinutes(1));
        await Assert.That(registration.RevokedAt).IsEqualTo(createdAt.AddMinutes(3));
        await Assert.That(() => registration.RotateControlPlaneCredential(
                "another-key",
                Convert.ToBase64String(new byte[32]),
                createdAt.AddDays(31),
                createdAt.AddMinutes(4)))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task UpgradePreflight_WhenRegistrationAndReadinessAreValid_ReturnsReady()
    {
        var registration = NewRegistration(DateTime.UtcNow.AddMinutes(-5));
        registration.MarkRegistered(DateTime.UtcNow.AddMinutes(-4));
        var registrationRepository = Substitute.For<IManagedControlPlaneRegistrationRepository>();
        registrationRepository.GetCurrentAsync(Arg.Any<CancellationToken>()).Returns(registration);
        var bootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
        bootstrapRepository.GetCurrent(Arg.Any<CancellationToken>()).Returns(new InstanceBootstrapState
        {
            Id = Guid.CreateVersion7(),
            IsCompleted = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            CompletedAt = DateTime.UtcNow.AddHours(-12)
        });
        var modeProvider = Substitute.For<IDeploymentModeProvider>();
        modeProvider.GetCurrentModeAsync(Arg.Any<CancellationToken>())
            .Returns(DeploymentMode.SingleTenant);
        var healthProbe = Substitute.For<IManagedEventHealthProbe>();
        healthProbe.CheckAsync(Arg.Any<CancellationToken>()).Returns(
            new ManagedEventHealthObservation("Healthy", DateTime.UtcNow));
        var handler = new GetManagementUpgradePreflightQueryHandler(
            Microsoft.Extensions.Options.Options.Create(new ManagedControlPlaneOptions { Enabled = true }),
            modeProvider,
            bootstrapRepository,
            registrationRepository,
            healthProbe);

        var result = await handler.Handle(
            new GetManagementUpgradePreflightQuery(
                "9999.0.0",
                ManagedControlPlaneContract.ManagementApiVersion),
            CancellationToken.None);

        await Assert.That(result.Ready).IsTrue();
        await Assert.That(result.Blockers).IsEmpty();
        await Assert.That(result.ManagedInstanceId).IsEqualTo(registration.ManagedInstanceId);
        await Assert.That(result.DeploymentMode).IsEqualTo(DeploymentMode.SingleTenant);
    }

    [Test]
    public async Task UpgradePostflight_WhenRuntimeVersionDiffers_ReturnsExplicitBlocker()
    {
        var registration = NewRegistration(DateTime.UtcNow.AddMinutes(-5));
        registration.MarkRegistered(DateTime.UtcNow.AddMinutes(-4));
        var registrationRepository = Substitute.For<IManagedControlPlaneRegistrationRepository>();
        registrationRepository.GetCurrentAsync(Arg.Any<CancellationToken>()).Returns(registration);
        var bootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
        bootstrapRepository.GetCurrent(Arg.Any<CancellationToken>()).Returns(new InstanceBootstrapState
        {
            Id = Guid.CreateVersion7(),
            IsCompleted = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            CompletedAt = DateTime.UtcNow.AddHours(-12)
        });
        var modeProvider = Substitute.For<IDeploymentModeProvider>();
        modeProvider.GetCurrentModeAsync(Arg.Any<CancellationToken>())
            .Returns(DeploymentMode.SingleTenant);
        var healthProbe = Substitute.For<IManagedEventHealthProbe>();
        healthProbe.CheckAsync(Arg.Any<CancellationToken>()).Returns(
            new ManagedEventHealthObservation("Healthy", DateTime.UtcNow));
        var handler = new GetManagementUpgradePostflightQueryHandler(
            Microsoft.Extensions.Options.Options.Create(new ManagedControlPlaneOptions { Enabled = true }),
            modeProvider,
            bootstrapRepository,
            registrationRepository,
            healthProbe);

        var result = await handler.Handle(
            new GetManagementUpgradePostflightQuery(
                "9999.0.0",
                ManagedControlPlaneContract.ManagementApiVersion),
            CancellationToken.None);

        await Assert.That(result.Verified).IsFalse();
        await Assert.That(result.Blockers.Select(blocker => blocker.Code))
            .Contains("event_version_mismatch");
    }

    [Test]
    public async Task UpgradePreflight_WhenTargetIsPrereleaseOfCurrentStableVersion_ReturnsOlderBlocker()
    {
        var registration = NewRegistration(DateTime.UtcNow.AddMinutes(-5));
        registration.MarkRegistered(DateTime.UtcNow.AddMinutes(-4));
        var registrationRepository = Substitute.For<IManagedControlPlaneRegistrationRepository>();
        registrationRepository.GetCurrentAsync(Arg.Any<CancellationToken>()).Returns(registration);
        var bootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
        bootstrapRepository.GetCurrent(Arg.Any<CancellationToken>()).Returns(new InstanceBootstrapState
        {
            Id = Guid.CreateVersion7(),
            IsCompleted = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            CompletedAt = DateTime.UtcNow.AddHours(-12)
        });
        var modeProvider = Substitute.For<IDeploymentModeProvider>();
        modeProvider.GetCurrentModeAsync(Arg.Any<CancellationToken>())
            .Returns(DeploymentMode.SingleTenant);
        var healthProbe = Substitute.For<IManagedEventHealthProbe>();
        healthProbe.CheckAsync(Arg.Any<CancellationToken>()).Returns(
            new ManagedEventHealthObservation("Healthy", DateTime.UtcNow));
        var handler = new GetManagementUpgradePreflightQueryHandler(
            Microsoft.Extensions.Options.Options.Create(new ManagedControlPlaneOptions { Enabled = true }),
            modeProvider,
            bootstrapRepository,
            registrationRepository,
            healthProbe);

        var result = await handler.Handle(
            new GetManagementUpgradePreflightQuery(
                "1.0.0-rc.1",
                ManagedControlPlaneContract.ManagementApiVersion),
            CancellationToken.None);

        await Assert.That(result.Ready).IsFalse();
        await Assert.That(result.Blockers.Select(blocker => blocker.Code))
            .Contains("target_version_older");
    }

    private static ManagedControlPlaneRegistration NewRegistration(DateTime createdAt) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            ManagedInstanceId = Guid.CreateVersion7(),
            EventInstanceId = Guid.CreateVersion7(),
            ControlPlaneEndpoint = "https://control.example.test",
            ManagementApiVersion = ManagedControlPlaneContract.ManagementApiVersion,
            EventVersion = "1.0.0",
            DeploymentMode = Explore.Domain.Enums.DeploymentMode.SingleTenant,
            RequestHash = new string('a', 64),
            EventToControlPlaneKeyId = "event-key",
            EventToControlPlaneSecretHash = Convert.ToBase64String(new byte[32]),
            ControlPlaneToEventKeyId = "control-plane-key",
            ControlPlaneToEventSecretHash = Convert.ToBase64String(new byte[32]),
            CredentialSecretBindingId = Guid.CreateVersion7(),
            EventToControlPlaneCredentialExpiresAt = createdAt.AddDays(30),
            ControlPlaneToEventCredentialExpiresAt = createdAt.AddDays(30),
            Status = ManagedControlPlaneRegistrationStatus.Pending,
            CreatedAt = createdAt
        };
}
