// ABOUTME: Unit tests for transactional tenant footer setting patches and governance-aware persistence.
// ABOUTME: Covers batch writes, rollback, lock skips, and one post-commit cache invalidation.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Footer;
using Explore.Application.Features.Footer.Handlers.Commands;
using Explore.Application.Features.Footer.Requests.Commands;
using Explore.Application.Models.Common;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Domain.Constants;
using Explore.Domain.Settings;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Footer.Commands;

public sealed class PatchTenantFooterSettingsCommandHandlerTests
{
    private const string Template = "community";
    private const string DescriptionText = "Community events and local services.";
    private const string CopyrightText = "© 2026 ISLAMU";

    private readonly List<string> _calls = [];
    private readonly IHierarchicalSettingsResolver _settingsResolver;
    private readonly ITenantSettingRepository _tenantSettings;
    private readonly RecordingUnitOfWork _unitOfWork;
    private readonly PatchTenantFooterSettingsCommandHandler _handler;

    public PatchTenantFooterSettingsCommandHandlerTests()
    {
        _settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        _tenantSettings = Substitute.For<ITenantSettingRepository>();
        _unitOfWork = new RecordingUnitOfWork(_calls);

        _settingsResolver.ResolveGroupAsync<FooterSettingGroup>(
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(new FooterSettingGroup());
        _settingsResolver.When(resolver => resolver.InvalidateCache(Arg.Any<SettingScope?>(), Arg.Any<Guid?>()))
            .Do(_ => _calls.Add("invalidated"));

        _handler = new PatchTenantFooterSettingsCommandHandler(
            _settingsResolver,
            _tenantSettings,
            _unitOfWork);
    }

    [Test]
    public async Task Handle_WhenMultipleLeavesAreSupplied_BatchesOneCommitAndInvalidatesOnce()
    {
        var tenantId = Guid.NewGuid();
        IReadOnlyList<FooterSocialLinkDto> socialLinks =
        [
            new() { Platform = "github", Url = "https://github.com/islamu", Label = "GitHub" }
        ];
        IReadOnlyCollection<TenantSettingOverrideUpsert> writes = [];
        _tenantSettings.UpsertManyForTenantAsync(
                tenantId,
                Arg.Any<IReadOnlyCollection<TenantSettingOverrideUpsert>>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                writes = call.Arg<IReadOnlyCollection<TenantSettingOverrideUpsert>>();
                _calls.Add("written");
                return Task.CompletedTask;
            });
        var command = new PatchTenantFooterSettingsCommand
        {
            UserId = Guid.NewGuid(),
            TenantId = tenantId,
            Patch = new PatchTenantFooterSettingsDto
            {
                General = new PatchTenantFooterGeneralDto
                {
                    Enabled = OptionalUpdate<bool>.Set(true)
                },
                Description = new PatchTenantFooterDescriptionDto
                {
                    Text = OptionalUpdate<string>.Set(DescriptionText)
                },
                SocialLinks = new PatchTenantFooterSocialLinksDto
                {
                    Items = OptionalUpdate<IReadOnlyList<FooterSocialLinkDto>>.Set(socialLinks)
                }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(writes.Count).IsEqualTo(3);
        await Assert.That(writes.Select(write => write.SettingKey)).IsEquivalentTo([
            GovernanceSettingKeys.Footer.Enabled,
            GovernanceSettingKeys.Footer.DescriptionText,
            GovernanceSettingKeys.Footer.SocialLinks
        ]);
        await Assert.That(writes.Single(write => write.SettingKey == GovernanceSettingKeys.Footer.SocialLinks).Value)
            .IsEqualTo(SettingValueSerializer.Serialize(socialLinks));
        await Assert.That(_unitOfWork.ExecutionCount).IsEqualTo(1);
        await Assert.That(_unitOfWork.CommitCount).IsEqualTo(1);
        await Assert.That(_unitOfWork.RollbackCount).IsEqualTo(0);
        await Assert.That(_calls).IsEquivalentTo(["written", "committed", "invalidated"]);
        _settingsResolver.Received(1).InvalidateCache(SettingScope.Tenant, tenantId);
    }

    [Test]
    public async Task Handle_WhenRepositoryWriteFails_RollsBackAndDoesNotInvalidate()
    {
        var tenantId = Guid.NewGuid();
        _tenantSettings.When(repository => repository.UpsertManyForTenantAsync(
                tenantId,
                Arg.Any<IReadOnlyCollection<TenantSettingOverrideUpsert>>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("Simulated write failure."));
        var command = CreateGeneralPatch(tenantId);

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _handler.Handle(command, CancellationToken.None));

        await Assert.That(_unitOfWork.ExecutionCount).IsEqualTo(1);
        await Assert.That(_unitOfWork.CommitCount).IsEqualTo(0);
        await Assert.That(_unitOfWork.RollbackCount).IsEqualTo(1);
        _settingsResolver.DidNotReceive().InvalidateCache(Arg.Any<SettingScope?>(), Arg.Any<Guid?>());
    }

    [Test]
    public async Task Handle_WhenDescriptionIsLocked_SkipsDescriptionAndWritesUnlockedLeaves()
    {
        var tenantId = Guid.NewGuid();
        SetupFooterLocks(lockDescription: true);
        IReadOnlyCollection<TenantSettingOverrideUpsert> writes = [];
        _tenantSettings.UpsertManyForTenantAsync(
                tenantId,
                Arg.Any<IReadOnlyCollection<TenantSettingOverrideUpsert>>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                writes = call.Arg<IReadOnlyCollection<TenantSettingOverrideUpsert>>();
                return Task.CompletedTask;
            });
        var command = new PatchTenantFooterSettingsCommand
        {
            UserId = Guid.NewGuid(),
            TenantId = tenantId,
            Patch = new PatchTenantFooterSettingsDto
            {
                General = new PatchTenantFooterGeneralDto
                {
                    Enabled = OptionalUpdate<bool>.Set(true)
                },
                Template = new PatchTenantFooterTemplateDto
                {
                    Value = OptionalUpdate<string>.Set(Template)
                },
                Description = new PatchTenantFooterDescriptionDto
                {
                    Text = OptionalUpdate<string>.Set(DescriptionText)
                }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(writes.Select(write => write.SettingKey)).IsEquivalentTo([
            GovernanceSettingKeys.Footer.Enabled,
            GovernanceSettingKeys.Footer.Template
        ]);
        await Assert.That(_unitOfWork.CommitCount).IsEqualTo(1);
        _settingsResolver.Received(1).InvalidateCache(SettingScope.Tenant, tenantId);
    }

    [Test]
    public async Task Handle_WhenEveryRequestedGovernedLeafIsLocked_SkipsTransactionAndInvalidatesOnce()
    {
        var tenantId = Guid.NewGuid();
        SetupFooterLocks(
            lockTemplate: true,
            lockDescription: true,
            lockSocialLinks: true,
            lockCopyright: true);
        var command = CreateGovernedPatch(tenantId);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _tenantSettings.DidNotReceiveWithAnyArgs().UpsertManyForTenantAsync(
            default,
            default!,
            default,
            default);
        await Assert.That(_unitOfWork.ExecutionCount).IsEqualTo(0);
        await Assert.That(_unitOfWork.CommitCount).IsEqualTo(0);
        _settingsResolver.Received(1).InvalidateCache(SettingScope.Tenant, tenantId);
        await _settingsResolver.Received(1).ResolveGroupAsync<FooterSettingGroup>(
            Arg.Any<SettingContext>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenPatchIsEmpty_FailsWithoutWritesOrCacheInvalidation()
    {
        var command = new PatchTenantFooterSettingsCommand
        {
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Patch = new PatchTenantFooterSettingsDto()
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors).Contains("At least one tenant footer settings field must be provided.");
        await _tenantSettings.DidNotReceiveWithAnyArgs().UpsertManyForTenantAsync(
            default,
            default!,
            default,
            default);
        await Assert.That(_unitOfWork.ExecutionCount).IsEqualTo(0);
        _settingsResolver.DidNotReceive().InvalidateCache(Arg.Any<SettingScope?>(), Arg.Any<Guid?>());
        await _settingsResolver.DidNotReceive().ResolveGroupAsync<FooterSettingGroup>(
            Arg.Any<SettingContext>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenPresentGroupHasNoLeaf_FailsWithoutTransactionOrInvalidation()
    {
        var command = new PatchTenantFooterSettingsCommand
        {
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Patch = new PatchTenantFooterSettingsDto
            {
                General = new PatchTenantFooterGeneralDto()
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(_unitOfWork.ExecutionCount).IsEqualTo(0);
        _settingsResolver.DidNotReceive().InvalidateCache(Arg.Any<SettingScope?>(), Arg.Any<Guid?>());
    }

    private void SetupFooterLocks(
        bool lockTemplate = false,
        bool lockDescription = false,
        bool lockSocialLinks = false,
        bool lockCopyright = false)
    {
        var settings = new Dictionary<string, ResolvedSetting>
        {
            [GovernanceSettingKeys.Footer.LockTenantTemplate] = CreateResolvedSetting(lockTemplate),
            [GovernanceSettingKeys.Footer.LockTenantDescription] = CreateResolvedSetting(lockDescription),
            [GovernanceSettingKeys.Footer.LockTenantSocialLinks] = CreateResolvedSetting(lockSocialLinks),
            [GovernanceSettingKeys.Footer.LockTenantCopyright] = CreateResolvedSetting(lockCopyright)
        };
        var group = new FooterSettingGroup();
        group.Populate(settings);

        _settingsResolver.ResolveGroupAsync<FooterSettingGroup>(
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(group);
    }

    private static ResolvedSetting CreateResolvedSetting(bool value) => new()
    {
        Value = SettingValueSerializer.Serialize(value)
    };

    private static PatchTenantFooterSettingsCommand CreateGeneralPatch(Guid tenantId) => new()
    {
        UserId = Guid.NewGuid(),
        TenantId = tenantId,
        Patch = new PatchTenantFooterSettingsDto
        {
            General = new PatchTenantFooterGeneralDto
            {
                Enabled = OptionalUpdate<bool>.Set(true)
            }
        }
    };

    private static PatchTenantFooterSettingsCommand CreateGovernedPatch(Guid tenantId) => new()
    {
        UserId = Guid.NewGuid(),
        TenantId = tenantId,
        Patch = new PatchTenantFooterSettingsDto
        {
            Template = new PatchTenantFooterTemplateDto
            {
                Value = OptionalUpdate<string>.Set(Template)
            },
            Description = new PatchTenantFooterDescriptionDto
            {
                Show = OptionalUpdate<bool>.Set(true),
                Text = OptionalUpdate<string>.Set(DescriptionText)
            },
            SocialLinks = new PatchTenantFooterSocialLinksDto
            {
                Show = OptionalUpdate<bool>.Set(true),
                Items = OptionalUpdate<IReadOnlyList<FooterSocialLinkDto>>.Set([])
            },
            Copyright = new PatchTenantFooterCopyrightDto
            {
                Text = OptionalUpdate<string>.Set(CopyrightText)
            }
        }
    };

    private sealed class RecordingUnitOfWork(List<string> calls) : IUnitOfWork
    {
        public int ExecutionCount { get; private set; }
        public int CommitCount { get; private set; }
        public int RollbackCount { get; private set; }

        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken ct = default)
        {
            ExecutionCount++;
            try
            {
                await operation(ct);
                CommitCount++;
                calls.Add("committed");
            }
            catch
            {
                RollbackCount++;
                calls.Add("rolled-back");
                throw;
            }
        }

        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<T> ExecuteSerializableAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default)
            => throw new NotSupportedException();
    }
}
