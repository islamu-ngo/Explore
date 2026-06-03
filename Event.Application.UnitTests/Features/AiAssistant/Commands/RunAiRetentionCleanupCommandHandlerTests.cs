// ABOUTME: Unit tests for tenant AI retention cleanup command orchestration.
// ABOUTME: Verifies setting-based cutoff calculation, dry-run propagation, and repository delegation.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.AiAssistant.Handlers.Commands;
using Explore.Application.Features.AiAssistant.Requests.Commands;
using Explore.Application.Models;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Domain;
using Explore.Domain.Constants;
using NSubstitute;

namespace Event.Application.UnitTests.Features.AiAssistant.Commands;

public sealed class RunAiRetentionCleanupCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly IAiConversationRepository _conversationRepository = Substitute.For<IAiConversationRepository>();
    private readonly IHierarchicalSettingsResolver _settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    public RunAiRetentionCleanupCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
    }

    [Test]
    public async Task Handle_UsesTenantRetentionDaysAndDelegatesDryRunCleanup()
    {
        var utcNow = new DateTime(2026, 06, 03, 12, 0, 0, DateTimeKind.Utc);
        var expected = new AiRetentionCleanupResult(
            utcNow.AddDays(-14),
            14,
            EligibleConversations: 2,
            RedactedConversations: 0,
            RedactedMessages: 0,
            RedactedRuns: 0,
            RedactedReferences: 0,
            RedactedProposedActions: 0,
            RedactedToolExecutions: 0,
            DryRun: true);

        _settingsResolver
            .ResolveGroupAsync<AiAssistantSettingGroup>(
                Arg.Is<SettingContext>(context => context.TenantId == _tenantId),
                Arg.Any<CancellationToken>())
            .Returns(CreateSettings(retentionDays: 14));
        _conversationRepository
            .RedactExpiredConversationsAsync(
                expected.CutoffUtc,
                14,
                utcNow,
                dryRun: true,
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var handler = new RunAiRetentionCleanupCommandHandler(
            _conversationRepository,
            _settingsResolver,
            _tenantContext);

        var result = await handler.Handle(
            new RunAiRetentionCleanupCommand { DryRun = true, UtcNow = utcNow },
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(expected);
        await _conversationRepository.Received(1).RedactExpiredConversationsAsync(
            expected.CutoffUtc,
            14,
            utcNow,
            dryRun: true,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenRetentionSettingIsInvalid_ClampsToOneDay()
    {
        var utcNow = new DateTime(2026, 06, 03, 12, 0, 0, DateTimeKind.Utc);
        var expected = new AiRetentionCleanupResult(
            utcNow.AddDays(-1),
            1,
            EligibleConversations: 0,
            RedactedConversations: 0,
            RedactedMessages: 0,
            RedactedRuns: 0,
            RedactedReferences: 0,
            RedactedProposedActions: 0,
            RedactedToolExecutions: 0,
            DryRun: false);

        _settingsResolver
            .ResolveGroupAsync<AiAssistantSettingGroup>(Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(retentionDays: 0));
        _conversationRepository
            .RedactExpiredConversationsAsync(
                expected.CutoffUtc,
                1,
                utcNow,
                dryRun: false,
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var handler = new RunAiRetentionCleanupCommandHandler(
            _conversationRepository,
            _settingsResolver,
            _tenantContext);

        var result = await handler.Handle(
            new RunAiRetentionCleanupCommand { UtcNow = utcNow },
            CancellationToken.None);

        await Assert.That(result.RetentionDays).IsEqualTo(1);
        await Assert.That(result.CutoffUtc).IsEqualTo(expected.CutoffUtc);
    }

    private static AiAssistantSettingGroup CreateSettings(int retentionDays)
    {
        var group = new AiAssistantSettingGroup();
        group.Populate(new Dictionary<string, ResolvedSetting>
        {
            [GovernanceSettingKeys.AiAssistant.RetentionDays] = new()
            {
                Key = GovernanceSettingKeys.AiAssistant.RetentionDays,
                Value = retentionDays.ToString(),
                ValueType = SettingValueType.Integer,
                Source = SettingSource.TenantOverride
            }
        });
        return group;
    }
}
