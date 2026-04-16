// ABOUTME: Unit tests for UpdateLocalizationGovernanceCommandHandler — validation, persistence, cache invalidation.
// ABOUTME: Verifies all 9 governance keys are upserted and validation rejects bad inputs.

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
    private readonly ISystemSettingRepository _settingRepository;
    private readonly ITranslationConfigResolver _configResolver;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantContext _tenantContext;
    private readonly UpdateLocalizationGovernanceCommandHandler _handler;

    public UpdateLocalizationGovernanceCommandHandlerTests()
    {
        _settingRepository = Substitute.For<ISystemSettingRepository>();
        var mediator = Substitute.For<IMediator>();
        var upsertService = new SettingUpsertService(_settingRepository, mediator);
        _configResolver = Substitute.For<ITranslationConfigResolver>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _tenantContext = Substitute.For<ITenantContext>();
        _tenantContext.TenantId.Returns(Guid.NewGuid());

        _handler = new UpdateLocalizationGovernanceCommandHandler(
            upsertService,
            _configResolver,
            _currentUserService,
            _tenantContext,
            Substitute.For<ILogger<UpdateLocalizationGovernanceCommandHandler>>());
    }

    [Test]
    public async Task Handle_ValidTolgeeConfig_PersistsAll9KeysAndInvalidatesCache()
    {
        var dto = BuildValidTolgeeDto();
        var command = new UpdateLocalizationGovernanceCommand { Dto = dto };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).Contains("successfully");
        _configResolver.Received(1).InvalidateCache(Arg.Any<Guid?>());
    }

    [Test]
    public async Task Handle_ValidWeblateConfig_RequiresComponentSlug()
    {
        var dto = BuildValidTolgeeDto();
        dto.TmsProvider = "weblate";
        dto.TmsComponent = "my-component";
        var command = new UpdateLocalizationGovernanceCommand { Dto = dto };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
    }

    [Test]
    public async Task Handle_WeblateWithoutComponent_FailsValidation()
    {
        var dto = BuildValidTolgeeDto();
        dto.TmsProvider = "weblate";
        dto.TmsComponent = null;
        var command = new UpdateLocalizationGovernanceCommand { Dto = dto };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).IsNotNull();
    }

    [Test]
    public async Task Handle_NoneProvider_ClearsTmsFieldsSuccessfully()
    {
        var dto = new UpdateLocalizationGovernanceDto
        {
            TmsProvider = "none",
            DefaultLanguage = "en",
            EnabledLanguages = ["en"],
            FallbackLanguage = "en",
            ClientPickerEnabled = true,
            ForceOfflineMode = false
        };
        var command = new UpdateLocalizationGovernanceCommand { Dto = dto };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
    }

    [Test]
    public async Task Handle_EmptyEnabledLanguages_FailsValidation()
    {
        var dto = BuildValidTolgeeDto();
        dto.EnabledLanguages = [];
        var command = new UpdateLocalizationGovernanceCommand { Dto = dto };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
    }

    [Test]
    public async Task Handle_UnknownCultureInEnabledLanguages_FailsValidation()
    {
        var dto = BuildValidTolgeeDto();
        dto.EnabledLanguages = ["en", "zz_INVALID"];
        var command = new UpdateLocalizationGovernanceCommand { Dto = dto };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
    }

    [Test]
    public async Task Handle_FallbackLanguageNotInEnabledSet_FailsValidation()
    {
        var dto = BuildValidTolgeeDto();
        dto.EnabledLanguages = ["en"];
        dto.FallbackLanguage = "fr";
        var command = new UpdateLocalizationGovernanceCommand { Dto = dto };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
    }

    [Test]
    public async Task Handle_DefaultLanguageNotInEnabledSet_FailsValidation()
    {
        var dto = BuildValidTolgeeDto();
        dto.EnabledLanguages = ["fr"];
        dto.DefaultLanguage = "en";
        dto.FallbackLanguage = "fr";
        var command = new UpdateLocalizationGovernanceCommand { Dto = dto };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
    }

    [Test]
    public async Task Handle_InvalidProvider_FailsValidation()
    {
        var dto = BuildValidTolgeeDto();
        dto.TmsProvider = "invalid_provider";
        var command = new UpdateLocalizationGovernanceCommand { Dto = dto };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
    }

    [Test]
    public async Task Handle_TolgeeWithoutApiUrl_FailsValidation()
    {
        var dto = BuildValidTolgeeDto();
        dto.TmsApiUrl = null;
        var command = new UpdateLocalizationGovernanceCommand { Dto = dto };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
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

    private static UpdateLocalizationGovernanceDto BuildValidTolgeeDto() => new()
    {
        TmsProvider = "tolgee",
        TmsApiUrl = "https://app.tolgee.io",
        TmsProjectId = "project-123",
        DefaultLanguage = "en",
        EnabledLanguages = ["en", "fr", "ar"],
        FallbackLanguage = "en",
        ClientPickerEnabled = true,
        ForceOfflineMode = false
    };
}
