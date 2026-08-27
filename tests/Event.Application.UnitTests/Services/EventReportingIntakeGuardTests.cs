// ABOUTME: Unit tests for tenant-effective event-reporting intake enforcement.
// ABOUTME: Verifies fail-closed resolution, effective settings, lock-resolved values, and cancellation.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.EventReporting;
using Explore.Application.Services;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Domain.Constants;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Services;

public sealed class EventReportingIntakeGuardTests
{
    private readonly IHierarchicalSettingsResolver _settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
    private readonly ILogger<EventReportingIntakeGuard> _logger = Substitute.For<ILogger<EventReportingIntakeGuard>>();

    [Test]
    public async Task ResolveAsync_WhenTenantIsEmpty_FailsClosedWithoutResolverAccess()
    {
        var result = await CreateGuard().ResolveAsync(Guid.Empty, CancellationToken.None);

        await Assert.That(result.TenantResolved).IsFalse();
        await Assert.That(result.IntakeEnabled).IsFalse();
        await Assert.That(result.ReasonCode).IsEqualTo(EventReportFailureCodes.TenantUnresolved);
        await _settingsResolver.DidNotReceive().ResolveGroupAsync<ReportingIntakeSettingGroup>(
            Arg.Any<SettingContext>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ResolveAsync_WhenNoOverrideExists_UsesEnabledDefault()
    {
        var tenantId = Guid.NewGuid();
        var group = new ReportingIntakeSettingGroup();
        _settingsResolver.ResolveGroupAsync<ReportingIntakeSettingGroup>(
                Arg.Is<SettingContext>(context => context != null && context.TenantId == tenantId),
                CancellationToken.None)
            .Returns(group);

        var result = await CreateGuard().ResolveAsync(tenantId, CancellationToken.None);

        await Assert.That(result.TenantResolved).IsTrue();
        await Assert.That(result.IntakeEnabled).IsTrue();
    }

    [Test]
    public async Task ResolveAsync_WhenEffectiveValueIsFalse_ReturnsExactIntakeDisabledCode()
    {
        var tenantId = Guid.NewGuid();
        ConfigureEffectiveIntake(tenantId, false);

        var result = await CreateGuard().ResolveAsync(tenantId, CancellationToken.None);

        await Assert.That(result.TenantResolved).IsTrue();
        await Assert.That(result.IntakeEnabled).IsFalse();
        await Assert.That(result.ReasonCode).IsEqualTo(EventReportFailureCodes.IntakeDisabled);
    }

    [Test]
    public async Task ResolveAsync_WhenResolverFails_FailsClosedWithIntakeDisabledCode()
    {
        var tenantId = Guid.NewGuid();
        _settingsResolver.ResolveGroupAsync<ReportingIntakeSettingGroup>(
                Arg.Is<SettingContext>(context => context != null && context.TenantId == tenantId),
                Arg.Any<CancellationToken>())
            .Returns<Task<ReportingIntakeSettingGroup>>(_ => throw new InvalidOperationException());

        var result = await CreateGuard().ResolveAsync(tenantId, CancellationToken.None);

        await Assert.That(result.TenantResolved).IsTrue();
        await Assert.That(result.IntakeEnabled).IsFalse();
        await Assert.That(result.ReasonCode).IsEqualTo(EventReportFailureCodes.IntakeDisabled);
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(state => LogStateContains(state, "TenantId", tenantId)),
            Arg.Is<Exception>(exception => exception is InvalidOperationException),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task ResolveAsync_WhenEffectiveValueIsLocked_UsesTheResolverEffectiveValue(bool effectiveIntakeEnabled)
    {
        var tenantId = Guid.NewGuid();
        ConfigureEffectiveIntake(tenantId, effectiveIntakeEnabled, locked: true);

        var result = await CreateGuard().ResolveAsync(tenantId, CancellationToken.None);

        await Assert.That(result.TenantResolved).IsTrue();
        await Assert.That(result.IntakeEnabled).IsEqualTo(effectiveIntakeEnabled);
        if (!effectiveIntakeEnabled)
            await Assert.That(result.ReasonCode).IsEqualTo(EventReportFailureCodes.IntakeDisabled);
    }

    [Test]
    public async Task ResolveAsync_WhenResolutionIsCancelled_PropagatesCancellation()
    {
        var tenantId = Guid.NewGuid();
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var cancellationToken = cancellationSource.Token;
        _settingsResolver.ResolveGroupAsync<ReportingIntakeSettingGroup>(
                Arg.Is<SettingContext>(context => context != null && context.TenantId == tenantId),
                cancellationToken)
            .Returns(Task.FromCanceled<ReportingIntakeSettingGroup>(cancellationToken));

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            CreateGuard().ResolveAsync(tenantId, cancellationToken));
    }

    private void ConfigureEffectiveIntake(Guid tenantId, bool intakeEnabled, bool locked = false)
    {
        var group = new ReportingIntakeSettingGroup();
        group.Populate(new Dictionary<string, ResolvedSetting>
        {
            [GovernanceSettingKeys.EventReporting.IntakeEnabled] = new()
            {
                Value = SettingValueSerializer.Serialize(intakeEnabled),
                IsLocked = locked
            }
        });
        _settingsResolver.ResolveGroupAsync<ReportingIntakeSettingGroup>(
                Arg.Is<SettingContext>(context => context != null && context.TenantId == tenantId),
                Arg.Any<CancellationToken>())
            .Returns(group);
    }

    private EventReportingIntakeGuard CreateGuard() => new(_settingsResolver, _logger);

    private static bool LogStateContains(object? state, string key, object? expectedValue)
    {
        return state is IEnumerable<KeyValuePair<string, object?>> values
            && values.Any(value => value.Key == key && Equals(value.Value, expectedValue));
    }
}
