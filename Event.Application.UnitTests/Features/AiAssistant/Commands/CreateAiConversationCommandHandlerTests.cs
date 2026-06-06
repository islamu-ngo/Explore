// ABOUTME: Unit tests for creating AI assistant conversation shells through Application handlers.
// ABOUTME: Verifies tenant governance, authentication, validation, and persistence boundaries.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Infrastructure.Ai;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Ai;
using Explore.Application.Features.AiAssistant.Handlers.Commands;
using Explore.Application.Features.AiAssistant.Requests.Commands;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Domain.Ai;
using Explore.Domain.Constants;
using NSubstitute;

namespace Event.Application.UnitTests.Features.AiAssistant.Commands;

public sealed class CreateAiConversationCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly IAiConversationRepository _conversationRepository = Substitute.For<IAiConversationRepository>();
    private readonly IHierarchicalSettingsResolver _settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    public CreateAiConversationCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUserService.IsAuthenticated.Returns(true);
        _currentUserService.UserId.Returns(_userId);
        _settingsResolver.ResolveGroupAsync<AiAssistantSettingGroup>(Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(enabled: true, provider: AiProviderDefaults.ProviderFake));
        _conversationRepository.Create(Arg.Any<AiConversation>()).Returns(call => call.Arg<AiConversation>());
    }

    [Test]
    public async Task Handle_WhenFakeProviderEnabled_CreatesConversationMetadataOnly()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new CreateAiConversationCommand
        {
            Conversation = new CreateAiConversationRequestDto
            {
                Title = "  Draft planning  ",
                ActorId = Guid.CreateVersion7()
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsNotEqualTo(Guid.Empty);

        await _conversationRepository.Received(1).Create(Arg.Is<AiConversation>(conversation =>
            conversation.TenantId == _tenantId &&
            conversation.UserId == _userId &&
            conversation.Title == "Draft planning" &&
            conversation.Provider == AiProviderDefaults.ProviderFake &&
            conversation.ModelId == AiProviderDefaults.FakeModelId &&
            conversation.Messages.Count == 0 &&
            conversation.Runs.Count == 0));
    }

    [Test]
    public async Task Handle_WhenUserIsUnauthenticated_FailsBeforePersistence()
    {
        _currentUserService.IsAuthenticated.Returns(false);
        _currentUserService.UserId.Returns((Guid?)null);

        var result = await CreateHandler().Handle(new CreateAiConversationCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("unauthenticated");
        await _conversationRepository.DidNotReceive().Create(Arg.Any<AiConversation>());
    }

    [Test]
    public async Task Handle_WhenTenantAiDisabled_FailsBeforePersistence()
    {
        _settingsResolver.ResolveGroupAsync<AiAssistantSettingGroup>(Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(enabled: false));

        var result = await CreateHandler().Handle(new CreateAiConversationCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("disabled");
        await _conversationRepository.DidNotReceive().Create(Arg.Any<AiConversation>());
    }

    [Test]
    public async Task Handle_WhenTitleTooLong_ReturnsValidationFailure()
    {
        var result = await CreateHandler().Handle(new CreateAiConversationCommand
        {
            Conversation = new CreateAiConversationRequestDto { Title = new string('x', 201) }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).IsNotNull();
        await _conversationRepository.DidNotReceive().Create(Arg.Any<AiConversation>());
    }

    private CreateAiConversationCommandHandler CreateHandler()
        => new(_conversationRepository, _settingsResolver, _tenantContext, _currentUserService);

    private static AiAssistantSettingGroup CreateSettings(
        bool enabled = false,
        string provider = AiProviderDefaults.ProviderNone,
        string endpointUrl = "",
        string apiKey = "",
        string modelId = "")
    {
        var settings = new Dictionary<string, ResolvedSetting>
        {
            [GovernanceSettingKeys.AiAssistant.Enabled] = Setting(GovernanceSettingKeys.AiAssistant.Enabled, enabled),
            [GovernanceSettingKeys.AiAssistant.Provider] = Setting(GovernanceSettingKeys.AiAssistant.Provider, provider),
            [GovernanceSettingKeys.AiAssistant.EndpointUrl] = Setting(GovernanceSettingKeys.AiAssistant.EndpointUrl, endpointUrl),
            [GovernanceSettingKeys.AiAssistant.ApiKey] = Setting(GovernanceSettingKeys.AiAssistant.ApiKey, apiKey),
            [GovernanceSettingKeys.AiAssistant.ModelId] = Setting(GovernanceSettingKeys.AiAssistant.ModelId, modelId)
        };

        var group = new AiAssistantSettingGroup();
        group.Populate(settings);
        return group;
    }

    private static ResolvedSetting Setting(string key, object value) => new()
    {
        Key = key,
        Value = System.Text.Json.JsonSerializer.Serialize(value),
        Source = SettingSource.SystemDefault,
        IsLocked = false
    };
}
