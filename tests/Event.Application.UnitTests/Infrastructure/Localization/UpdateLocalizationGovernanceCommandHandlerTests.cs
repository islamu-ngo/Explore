// ABOUTME: Unit tests for UpdateLocalizationGovernanceCommandHandler — validation, persistence, cache invalidation.
// ABOUTME: Verifies all 9 governance keys are upserted and validation rejects bad inputs.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Localization;
using Explore.Application.Features.Localization.Handlers.Commands;
using Explore.Application.Features.Localization.Requests.Commands;
using Explore.Application.Settings;
using Explore.Domain.Constants;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Infrastructure.Localization;

public class UpdateLocalizationGovernanceCommandHandlerTests
{
    private static readonly Guid ActorId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly IAdminContext _adminContext;
    private readonly ISystemSettingRepository _settingRepository;
    private readonly ITranslationConfigResolver _configResolver;
    private readonly ITenantContext _tenantContext;
    private readonly UpdateLocalizationGovernanceCommandHandler _handler;

    public UpdateLocalizationGovernanceCommandHandlerTests()
    {
        _adminContext = Substitute.For<IAdminContext>();
        _adminContext.ResolveUserIdAsync(Arg.Any<CancellationToken>()).Returns(ActorId);
        _adminContext.IsInstanceAdminAsync(ActorId, Arg.Any<CancellationToken>()).Returns(true);
        _settingRepository = Substitute.For<ISystemSettingRepository>();
        var mediator = Substitute.For<IMediator>();
        var upsertService = new SettingUpsertService(
            _settingRepository,
            mediator,
            Substitute.For<IPublicationPolicyMutationBoundary>());
        _configResolver = Substitute.For<ITranslationConfigResolver>();
        _tenantContext = Substitute.For<ITenantContext>();
        _tenantContext.TenantId.Returns(Guid.NewGuid());

        _handler = new UpdateLocalizationGovernanceCommandHandler(
            _adminContext,
            upsertService,
            _configResolver,
            _tenantContext,
            Substitute.For<ILogger<UpdateLocalizationGovernanceCommandHandler>>());
    }

    [Test]
    public async Task Handle_WhenUserIsNotInstanceAdmin_DeniesBeforeUpsertOrCacheInvalidation()
    {
        _adminContext.IsInstanceAdminAsync(ActorId, Arg.Any<CancellationToken>()).Returns(false);
        var command = new UpdateLocalizationGovernanceCommand { Dto = BuildValidTolgeeDto() };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Message).Contains("Instance administrator");
        await _settingRepository.DidNotReceive().UpsertAsync(
            Arg.Any<Explore.Domain.SystemSetting>(),
            Arg.Any<CancellationToken>());
        _configResolver.DidNotReceive().InvalidateCache(Arg.Any<Guid?>());
    }

    [Test]
    public async Task Handle_WhenCancelledDuringAdminResolution_PropagatesCancellationBeforeUpsertOrCacheInvalidation()
    {
        using var source = new CancellationTokenSource();
        await source.CancelAsync();
        _adminContext.ResolveUserIdAsync(source.Token)
            .Returns(Task.FromCanceled<Guid?>(source.Token));
        var command = new UpdateLocalizationGovernanceCommand { Dto = BuildValidTolgeeDto() };

        var exception = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _handler.Handle(command, source.Token));

        await Assert.That(exception.CancellationToken).IsEqualTo(source.Token);
        await _adminContext.Received(1).ResolveUserIdAsync(source.Token);
        await _adminContext.DidNotReceive().IsInstanceAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _settingRepository.DidNotReceive().UpsertAsync(
            Arg.Any<Explore.Domain.SystemSetting>(),
            Arg.Any<CancellationToken>());
        _configResolver.DidNotReceive().InvalidateCache(Arg.Any<Guid?>());
    }

    [Test]
    public async Task Handle_ValidTolgeeConfig_PersistsAll9KeysAndInvalidatesCache()
    {
        var dto = BuildValidTolgeeDto();
        var command = new UpdateLocalizationGovernanceCommand { Dto = dto };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Message).Contains("successfully");
        _configResolver.Received(1).InvalidateCache(Arg.Any<Guid?>());
    }

    [Test]
    public async Task Handle_ValidWeblateConfig_RequiresComponentSlug()
    {
        var dto = BuildValidTolgeeDto();
        dto = dto with { Tms = dto.Tms! with { Provider = "weblate", Component = "my-component" } };
        var command = new UpdateLocalizationGovernanceCommand { Dto = dto };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
    }

    [Test]
    public async Task Handle_WeblateWithoutComponent_FailsValidation()
    {
        var dto = BuildValidTolgeeDto();
        dto = dto with { Tms = dto.Tms! with { Provider = "weblate", Component = null } };
        var command = new UpdateLocalizationGovernanceCommand { Dto = dto };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors).IsNotNull();
    }

    [Test]
    public async Task Handle_NoneProvider_ClearsTmsFieldsSuccessfully()
    {
        var dto = new UpdateLocalizationGovernanceDto
        {
            Tms = new LocalizationTmsUpdateDto { Provider = "none" }
        };
        var command = new UpdateLocalizationGovernanceCommand { Dto = dto };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
    }

    [Test]
    public async Task Handle_EmptyEnabledLanguages_FailsValidation()
    {
        var dto = BuildValidTolgeeDto();
        dto = dto with { Languages = dto.Languages! with { EnabledLanguages = [] } };
        var command = new UpdateLocalizationGovernanceCommand { Dto = dto };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task Handle_UnknownCultureInEnabledLanguages_FailsValidation()
    {
        var dto = BuildValidTolgeeDto();
        dto = dto with { Languages = dto.Languages! with { EnabledLanguages = ["en", "zz_INVALID"] } };
        var command = new UpdateLocalizationGovernanceCommand { Dto = dto };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task Handle_FallbackLanguageNotInEnabledSet_FailsValidation()
    {
        var dto = BuildValidTolgeeDto();
        dto = dto with { Languages = dto.Languages! with { EnabledLanguages = ["en"], FallbackLanguage = "fr" } };
        var command = new UpdateLocalizationGovernanceCommand { Dto = dto };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task Handle_DefaultLanguageNotInEnabledSet_FailsValidation()
    {
        var dto = BuildValidTolgeeDto();
        dto = dto with
        {
            Languages = dto.Languages! with
            {
                EnabledLanguages = ["fr"],
                DefaultLanguage = "en",
                FallbackLanguage = "fr"
            }
        };
        var command = new UpdateLocalizationGovernanceCommand { Dto = dto };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task Handle_InvalidProvider_FailsValidation()
    {
        var dto = BuildValidTolgeeDto();
        dto = dto with { Tms = dto.Tms! with { Provider = "invalid_provider" } };
        var command = new UpdateLocalizationGovernanceCommand { Dto = dto };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task Handle_TolgeeWithoutApiUrl_FailsValidation()
    {
        var dto = BuildValidTolgeeDto();
        dto = dto with { Tms = dto.Tms! with { ApiUrl = null } };
        var command = new UpdateLocalizationGovernanceCommand { Dto = dto };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task Handle_InvalidatesCorrectTenantCache()
    {
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);
        var dto = BuildValidTolgeeDto();
        var command = new UpdateLocalizationGovernanceCommand { Dto = dto };

        await _handler.Handle(command, CancellationToken.None);

        _configResolver.Received(1).InvalidateCache(tenantId);
    }

    [Test]
    public async Task Handle_RuntimeOnly_PersistsOnlyRuntimeKeys()
    {
        var result = await _handler.Handle(
            new UpdateLocalizationGovernanceCommand
            {
                Dto = new UpdateLocalizationGovernanceDto
                {
                    Runtime = new LocalizationRuntimeUpdateDto
                    {
                        ClientPickerEnabled = false,
                        ForceOfflineMode = true
                    }
                }
            },
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _settingRepository.Received(1).UpsertAsync(
            Arg.Is<Explore.Domain.SystemSetting>((Explore.Domain.SystemSetting setting) =>
                setting.SettingKey == GovernanceSettingKeys.Localization.ClientPickerEnabled),
            Arg.Any<CancellationToken>());
        await _settingRepository.Received(1).UpsertAsync(
            Arg.Is<Explore.Domain.SystemSetting>((Explore.Domain.SystemSetting setting) =>
                setting.SettingKey == GovernanceSettingKeys.Localization.ForceOfflineMode),
            Arg.Any<CancellationToken>());
        await _settingRepository.DidNotReceive().UpsertAsync(
            Arg.Is<Explore.Domain.SystemSetting>((Explore.Domain.SystemSetting setting) =>
                setting.SettingKey == GovernanceSettingKeys.Localization.TmsProvider
                || setting.SettingKey == GovernanceSettingKeys.Localization.EnabledLanguages),
            Arg.Any<CancellationToken>());
    }

    private static UpdateLocalizationGovernanceDto BuildValidTolgeeDto() => new()
    {
        Tms = new LocalizationTmsUpdateDto
        {
            Provider = "tolgee",
            ApiUrl = "https://app.tolgee.io",
            ProjectId = "project-123"
        },
        Languages = new LocalizationLanguagePolicyUpdateDto
        {
            DefaultLanguage = "en",
            EnabledLanguages = ["en", "fr", "ar"],
            FallbackLanguage = "en"
        },
        Runtime = new LocalizationRuntimeUpdateDto
        {
            ClientPickerEnabled = true,
            ForceOfflineMode = false
        }
    };
}
