// ABOUTME: Unit tests for scheduled AI retention cleanup orchestration.
// ABOUTME: Verifies tenant iteration, dry-run propagation, and bounded aggregate failure handling.

using System.Diagnostics.Metrics;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Models;
using Explore.Application.Models.Tenants;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Application.Telemetry;
using Explore.Domain.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Infrastructure;

public sealed class AiRetentionCleanupServiceTests
{
    [Test]
    public async Task CleanupAllTenantsAsync_WhenDryRun_IteratesActiveTenantsWithTenantContext()
    {
        var tenantId = Guid.CreateVersion7();
        var utcNow = new DateTime(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);
        var repository = Substitute.For<IAiConversationRepository>();
        repository.RedactExpiredConversationsAsync(
                utcNow.AddDays(-14),
                14,
                utcNow,
                true,
                Arg.Any<CancellationToken>())
            .Returns(new AiRetentionCleanupResult(
                utcNow.AddDays(-14),
                14,
                EligibleConversations: 2,
                RedactedConversations: 0,
                RedactedMessages: 0,
                RedactedRuns: 0,
                RedactedReferences: 0,
                RedactedProposedActions: 0,
                RedactedToolExecutions: 0,
                DryRun: true));

        var tenantAccessor = Substitute.For<ITenantContextAccessor>();
        var service = CreateService(
            [tenantId],
            repository,
            tenantAccessor,
            retentionDays: 14,
            new AiRetentionCleanupSettings { DryRun = true });

        var result = await service.CleanupAllTenantsAsync(utcNow, CancellationToken.None);

        await Assert.That(result.DryRun).IsTrue();
        await Assert.That(result.TenantCount).IsEqualTo(1);
        await Assert.That(result.SucceededTenantCount).IsEqualTo(1);
        await Assert.That(result.FailedTenantCount).IsEqualTo(0);
        await Assert.That(result.EligibleConversations).IsEqualTo(2);
        tenantAccessor.Received(1).SetTenant(tenantId);
        tenantAccessor.Received(1).Clear();
    }

    [Test]
    public async Task CleanupAllTenantsAsync_WhenOneTenantFails_ContinuesAndReportsPartialFailure()
    {
        var failingTenantId = Guid.CreateVersion7();
        var succeedingTenantId = Guid.CreateVersion7();
        var utcNow = new DateTime(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);
        var repository = Substitute.For<IAiConversationRepository>();
        repository.RedactExpiredConversationsAsync(
                Arg.Any<DateTime>(),
                Arg.Any<int>(),
                utcNow,
                false,
                Arg.Any<CancellationToken>())
            .Returns(
                _ => throw new InvalidOperationException("tenant cleanup failed"),
                _ => new AiRetentionCleanupResult(
                    utcNow.AddDays(-30),
                    30,
                    EligibleConversations: 1,
                    RedactedConversations: 1,
                    RedactedMessages: 2,
                    RedactedRuns: 0,
                    RedactedReferences: 0,
                    RedactedProposedActions: 0,
                    RedactedToolExecutions: 0,
                    DryRun: false));

        var service = CreateService(
            [failingTenantId, succeedingTenantId],
            repository,
            Substitute.For<ITenantContextAccessor>(),
            retentionDays: 30,
            new AiRetentionCleanupSettings());

        var result = await service.CleanupAllTenantsAsync(utcNow, CancellationToken.None);

        await Assert.That(result.TenantCount).IsEqualTo(2);
        await Assert.That(result.SucceededTenantCount).IsEqualTo(1);
        await Assert.That(result.FailedTenantCount).IsEqualTo(1);
        await Assert.That(result.RedactedConversations).IsEqualTo(1);
        await Assert.That(result.RedactedMessages).IsEqualTo(2);
    }

    private static AiRetentionCleanupService CreateService(
        IReadOnlyList<Guid> tenantIds,
        IAiConversationRepository repository,
        ITenantContextAccessor tenantAccessor,
        int retentionDays,
        AiRetentionCleanupSettings settings)
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IHierarchicalSettingsResolver>());
        services.AddSingleton(repository);
        services.AddSingleton(tenantAccessor);
        services.AddSingleton<ITenantLookupSource>(new TestTenantLookupSource(tenantIds));
        var provider = services.BuildServiceProvider();

        var settingsResolver = provider.GetRequiredService<IHierarchicalSettingsResolver>();
        settingsResolver.ResolveGroupAsync<AiAssistantSettingGroup>(
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(CreateSettings(retentionDays));

        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter(BusinessMetrics.MeterName));

        return new AiRetentionCleanupService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(settings),
            new BusinessMetrics(meterFactory),
            NullLogger<AiRetentionCleanupService>.Instance);
    }

    private static AiAssistantSettingGroup CreateSettings(int retentionDays)
    {
        var settings = new AiAssistantSettingGroup();
        settings.Populate(new Dictionary<string, ResolvedSetting>
        {
            [GovernanceSettingKeys.AiAssistant.RetentionDays] = new()
            {
                Key = GovernanceSettingKeys.AiAssistant.RetentionDays,
                Value = retentionDays.ToString()
            }
        });
        return settings;
    }

    private sealed class TestTenantLookupSource(IReadOnlyList<Guid> tenantIds) : ITenantLookupSource
    {
        public Task<IReadOnlyList<TenantLookupRecord>> GetTenantLookupsAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<TenantLookupRecord> records = tenantIds
                .Select((tenantId, index) => new TenantLookupRecord
                {
                    TenantId = tenantId,
                    Slug = $"tenant-{index}"
                })
                .ToList();

            return Task.FromResult(records);
        }
    }
}
