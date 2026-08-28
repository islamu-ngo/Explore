// ABOUTME: RED specifications for the current-tenant reporting-intake policy query contract.
// ABOUTME: Requires canonical authorization facts, effective metadata, disablement evaluation, isolation, and cancellation.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.DTOs.EventReporting;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventReporting.Handlers.Queries;
using Explore.Application.Features.EventReporting.Requests.Queries;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Domain;
using Explore.Domain.Constants;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventReporting.Queries;

public sealed class GetTenantReportingIntakePolicyQueryHandlerTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IHierarchicalSettingsResolver _settings = Substitute.For<IHierarchicalSettingsResolver>();

    public GetTenantReportingIntakePolicyQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
    }

    [Test]
    public async Task RequestMetadata_UsesCanonicalTenantSettingViewCapability()
    {
        var request = new GetTenantReportingIntakePolicyQuery(_tenantId);
        var secure = (ISecureRequest)request;
        AuthorizeResourceAttribute authorization = typeof(GetTenantReportingIntakePolicyQuery)
            .GetCustomAttributes(typeof(AuthorizeResourceAttribute), inherit: false)
            .Cast<AuthorizeResourceAttribute>()
            .Single();

        await Assert.That(authorization.Resource).IsEqualTo(ResourceKinds.TenantSetting);
        await Assert.That(authorization.Action).IsEqualTo(AuthorizationActions.TenantSettings.View);
        await Assert.That(secure.ResourceId).IsEqualTo(GovernanceSettingKeys.EventReporting.IntakeEnabled);
        await Assert.That(secure.AuthorizationFacts).IsEqualTo(new TenantSettingAuthorizationFacts(
            _tenantId,
            GovernanceSettingKeys.EventReporting.IntakeEnabled));
    }

    [Test]
    public async Task Handle_ReturnsEffectiveMetadataAndEvaluatesTheProposedDisabledState()
    {
        _settings.ResolveWithMetadataAsync(
                GovernanceSettingKeys.EventReporting.IntakeEnabled,
                Arg.Is<SettingContext>(context => context.TenantId == _tenantId),
                Arg.Any<CancellationToken>())
            .Returns(new ResolvedSetting
            {
                Key = GovernanceSettingKeys.EventReporting.IntakeEnabled,
                Value = "true",
                ValueType = SettingValueType.Boolean,
                Source = SettingSource.TenantOverride,
                IsLocked = false
            });
        _settings.ResolveGroupAsync<EventSettingGroup>(
                Arg.Is<SettingContext>(context => context.TenantId == _tenantId),
                Arg.Any<CancellationToken>())
            .Returns(EventPolicy(
                requireApproval: false,
                userSubmissionEnabled: true,
                organizationSubmissionEnabled: false,
                groupSubmissionEnabled: false));

        TenantReportingIntakePolicyDto result = await CreateHandler().Handle(
            new GetTenantReportingIntakePolicyQuery(_tenantId),
            CancellationToken.None);

        await Assert.That(result.TenantId).IsEqualTo(_tenantId);
        await Assert.That(result.Enabled).IsTrue();
        await Assert.That(result.Source).IsEqualTo(SettingSource.TenantOverride);
        await Assert.That(result.IsLockedByInstance).IsFalse();
        await Assert.That(result.CanDisable).IsFalse();
        await Assert.That(result.ReasonCode).IsEqualTo(ReportingIntakePolicyReasonCodes.UnsafePublicationPolicy);
        await Assert.That(result.Reason)
            .IsEqualTo("Reporting intake cannot be disabled while an ordinary submission path is open.");
    }

    [Test]
    public async Task Handle_WhenApprovalProtectsPublication_AllowsProposedDisablementWithStableReason()
    {
        UseResolvedIntake(enabled: true, SettingSource.SystemDefault, isLocked: false);
        _settings.ResolveGroupAsync<EventSettingGroup>(
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(EventPolicy(
                requireApproval: true,
                userSubmissionEnabled: true,
                organizationSubmissionEnabled: true,
                groupSubmissionEnabled: true));

        TenantReportingIntakePolicyDto result = await CreateHandler().Handle(
            new GetTenantReportingIntakePolicyQuery(_tenantId),
            CancellationToken.None);

        await Assert.That(result.CanDisable).IsTrue();
        await Assert.That(result.ReasonCode).IsEqualTo(ReportingIntakePolicyReasonCodes.ProtectedByApproval);
        await Assert.That(result.Reason).IsEqualTo("Publication is protected by approval.");
    }

    [Test]
    public async Task Handle_WhenInstanceLocked_ReportsLockAndNeverAdvertisesDisablement()
    {
        UseResolvedIntake(enabled: true, SettingSource.SystemLocked, isLocked: true);
        _settings.ResolveGroupAsync<EventSettingGroup>(
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(EventPolicy(
                requireApproval: true,
                userSubmissionEnabled: false,
                organizationSubmissionEnabled: false,
                groupSubmissionEnabled: false));

        TenantReportingIntakePolicyDto result = await CreateHandler().Handle(
            new GetTenantReportingIntakePolicyQuery(_tenantId),
            CancellationToken.None);

        await Assert.That(result.Source).IsEqualTo(SettingSource.SystemLocked);
        await Assert.That(result.IsLockedByInstance).IsTrue();
        await Assert.That(result.CanDisable).IsFalse();
        await Assert.That(result.ReasonCode).IsEqualTo("event_reporting_policy_locked");
        await Assert.That(result.Reason).IsNotNull().And.IsNotEmpty();
    }

    [Test]
    public async Task Handle_WhenRequestTenantDiffersFromAmbientTenant_DeniesBeforeSettingsRead()
    {
        var otherTenantId = Guid.CreateVersion7();

        await Assert.ThrowsAsync<AuthorizationException>(() => CreateHandler().Handle(
            new GetTenantReportingIntakePolicyQuery(otherTenantId),
            CancellationToken.None));

        await _settings.DidNotReceiveWithAnyArgs().ResolveWithMetadataAsync(default!, default, default);
        await _settings.DidNotReceiveWithAnyArgs().ResolveGroupAsync<EventSettingGroup>(default, default);
    }

    [Test]
    public async Task Handle_WhenResolutionIsCancelled_PropagatesTheExactCancellationToken()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        _settings.ResolveWithMetadataAsync(
                GovernanceSettingKeys.EventReporting.IntakeEnabled,
                Arg.Any<SettingContext>(),
                cancellation.Token)
            .Returns(Task.FromCanceled<ResolvedSetting?>(cancellation.Token));

        OperationCanceledException exception = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            CreateHandler().Handle(new GetTenantReportingIntakePolicyQuery(_tenantId), cancellation.Token));

        await Assert.That(exception.CancellationToken).IsEqualTo(cancellation.Token);
        await _settings.Received(1).ResolveWithMetadataAsync(
            GovernanceSettingKeys.EventReporting.IntakeEnabled,
            Arg.Is<SettingContext>(context => context.TenantId == _tenantId),
            cancellation.Token);
        await _settings.DidNotReceiveWithAnyArgs().ResolveGroupAsync<EventSettingGroup>(default, default);
    }

    private GetTenantReportingIntakePolicyQueryHandler CreateHandler() => new(_tenantContext, _settings);

    private void UseResolvedIntake(bool enabled, SettingSource source, bool isLocked)
    {
        _settings.ResolveWithMetadataAsync(
                GovernanceSettingKeys.EventReporting.IntakeEnabled,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(new ResolvedSetting
            {
                Key = GovernanceSettingKeys.EventReporting.IntakeEnabled,
                Value = enabled ? "true" : "false",
                ValueType = SettingValueType.Boolean,
                Source = source,
                IsLocked = isLocked
            });
    }

    private static EventSettingGroup EventPolicy(
        bool requireApproval,
        bool userSubmissionEnabled,
        bool organizationSubmissionEnabled,
        bool groupSubmissionEnabled)
    {
        var group = new EventSettingGroup();
        group.Populate(new Dictionary<string, ResolvedSetting>(StringComparer.Ordinal)
        {
            [GovernanceSettingKeys.Events.RequireApproval] = BooleanSetting(
                GovernanceSettingKeys.Events.RequireApproval,
                requireApproval),
            [GovernanceSettingKeys.Events.UserSubmissionEnabled] = BooleanSetting(
                GovernanceSettingKeys.Events.UserSubmissionEnabled,
                userSubmissionEnabled),
            [GovernanceSettingKeys.Events.OrganizationSubmissionEnabled] = BooleanSetting(
                GovernanceSettingKeys.Events.OrganizationSubmissionEnabled,
                organizationSubmissionEnabled),
            [GovernanceSettingKeys.Events.GroupSubmissionEnabled] = BooleanSetting(
                GovernanceSettingKeys.Events.GroupSubmissionEnabled,
                groupSubmissionEnabled)
        });
        return group;
    }

    private static ResolvedSetting BooleanSetting(string key, bool value) => new()
    {
        Key = key,
        Value = value ? "true" : "false",
        ValueType = SettingValueType.Boolean,
        Source = SettingSource.SystemDefault
    };
}
