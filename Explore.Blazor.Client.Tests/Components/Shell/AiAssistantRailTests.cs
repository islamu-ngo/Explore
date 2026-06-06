// ABOUTME: bUnit coverage for the shell AI assistant rail UI.
// ABOUTME: Verifies generated-client service usage and HAL-gated proposed action affordances.

using Explore.Blazor.Client.Components.Shell;
using Explore.Blazor.Client.Contracts.Services.Ai;
using Explore.Blazor.Client.Services.Ai;

namespace Explore.Blazor.Client.Tests.Components.Shell;

public sealed class AiAssistantRailTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();
    private readonly IAiAssistantClientService _clientService = Substitute.For<IAiAssistantClientService>();
    private readonly AiAssistantState _shellState = new();
    private readonly AiAssistantConversationState _conversationState = new();

    public AiAssistantRailTests()
    {
        _ctx.Services.AddSingleton(_clientService);
        _ctx.Services.AddSingleton(_shellState);
        _ctx.Services.AddSingleton(_conversationState);
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    [Test]
    public async Task Render_WhenAssistantIsUnavailable_DoesNotRenderRailOrLoadConversations()
    {
        var cut = _ctx.RenderMudComponent<AiAssistantRail>();

        await Assert.That(cut.FindAll("[data-testid='shell-ai-rail']")).IsEmpty();
        await _clientService.DidNotReceive().GetConversationCollectionAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Render_WhenAssistantIsAvailable_LoadsConversationsAndMessages()
    {
        var conversationId = Guid.CreateVersion7();
        _clientService.GetConversationCollectionAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalCollectionResourceOfAiConversationSummaryDto?>(
                CreateConversationCollection(
                [
                    new() { Id = conversationId, Title = "Event planning", Status = "Active" }
                ])));
        _clientService.GetConversationAsync(conversationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalResourceOfAiConversationDto?>(CreateConversation(
                conversationId,
                "Event planning",
                [
                    new Messages2 { Role = "User", Sequence = 1, Content = "Plan an iftar" },
                    new Messages2 { Role = "Assistant", Sequence = 2, Content = "I can help with that." }
                ])));
        _shellState.SetPolicy(tenantAvailable: true, allowAnonymousAccess: false, isAuthenticated: true);
        _shellState.Open();

        var cut = _ctx.RenderMudComponent<AiAssistantRail>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Event planning", StringComparison.Ordinal)
                || !cut.Markup.Contains("Plan an iftar", StringComparison.Ordinal)
                || !cut.Markup.Contains("I can help with that.", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected rail to render loaded conversation details.");
            }
        });

        await _clientService.Received(1).GetConversationCollectionAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _clientService.Received(1).GetConversationAsync(conversationId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Render_OnlyShowsProposalButtonsWhenHalLinksExist()
    {
        var conversationId = Guid.CreateVersion7();
        var linkedActionId = Guid.CreateVersion7();
        var staleActionId = Guid.CreateVersion7();
        _conversationState.SelectConversation(new HalResourceOfAiConversationDto
        {
            Id = conversationId,
            Title = "Proposal review",
            ProposedActions =
            [
                new ProposedActions2
                {
                    Id = linkedActionId,
                    Kind = "CreateEventDraft",
                    Status = "Proposed",
                    _links = new Dictionary<string, Anonymous59>
                    {
                        ["confirm-action"] = new() { Href = "/confirm", Method = "POST" },
                        ["reject-action"] = new() { Href = "/reject", Method = "POST" }
                    }
                },
                new ProposedActions2
                {
                    Id = staleActionId,
                    Kind = "CreateEventDraft",
                    Status = "Executed"
                }
            ]
        });
        _clientService.GetConversationCollectionAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalCollectionResourceOfAiConversationSummaryDto?>(CreateConversationCollection([])));
        _clientService.ConfirmProposedActionAsync(conversationId, linkedActionId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new AiAssistantCommandResult(true, Guid.CreateVersion7(), "Confirmed", null, [])));
        _clientService.GetConversationAsync(conversationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalResourceOfAiConversationDto?>(_conversationState.SelectedConversation));
        _shellState.SetPolicy(tenantAvailable: true, allowAnonymousAccess: false, isAuthenticated: true);
        _shellState.Open();

        var cut = _ctx.RenderMudComponent<AiAssistantRail>();

        cut.WaitForAssertion(() =>
        {
            if (cut.FindAll("[data-testid='ai-rail-confirm-action']").Count != 1
                || cut.FindAll("[data-testid='ai-rail-reject-action']").Count != 1)
            {
                throw new InvalidOperationException("Expected only HAL-linked action buttons.");
            }
        });

        await cut.Find("[data-testid='ai-rail-confirm-action']").ClickAsync(new MouseEventArgs());

        await _clientService.Received(1).ConfirmProposedActionAsync(
            conversationId,
            linkedActionId,
            Arg.Is<string?>(value => value != null && value.StartsWith("blazor-ai-confirm-", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReferenceSearch_SelectsAndRemovesHalLinkedReference()
    {
        var conversationId = Guid.CreateVersion7();
        var referenceId = Guid.CreateVersion7();
        _conversationState.SelectConversation(new HalResourceOfAiConversationDto { Id = conversationId, Title = "References" });
        _clientService.GetConversationCollectionAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalCollectionResourceOfAiConversationSummaryDto?>(CreateConversationCollection([])));
        _clientService.SearchReferencesAsync("iftar", Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<HalResourceOfAiReferenceSearchResultDto>>(
            [
                new()
                {
                    Kind = "Event",
                    ReferenceId = referenceId,
                    DisplayName = "Community Iftar",
                    _links = new Dictionary<string, Anonymous8>
                    {
                        ["event"] = new() { Href = $"/api/events/{referenceId}", Method = "GET" }
                    }
                }
            ]));
        _shellState.SetPolicy(tenantAvailable: true, allowAnonymousAccess: false, isAuthenticated: true);
        _shellState.Open();

        var cut = _ctx.RenderMudComponent<AiAssistantRail>();

        await cut.Find("[data-testid='ai-rail-reference-search']").InputAsync(new ChangeEventArgs { Value = "iftar" });
        await cut.Find("[data-testid='ai-rail-reference-search-submit']").ClickAsync(new MouseEventArgs());

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Community Iftar", StringComparison.Ordinal)
                || !cut.Markup.Contains("Event link available", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected HAL-linked reference result.");
            }
        });

        await cut.Find("[data-testid='ai-rail-reference-result']").ClickAsync(new MouseEventArgs());
        await Assert.That(_conversationState.SelectedReferences.Count).IsEqualTo(1);

        await cut.Find("[data-testid='ai-reference-chip']").ClickAsync(new MouseEventArgs());
        await Assert.That(_conversationState.SelectedReferences).IsEmpty();
    }

    [Test]
    public async Task NewConversation_WhenCreateSucceeds_LoadsCreatedConversation()
    {
        var conversationId = Guid.CreateVersion7();
        _clientService.GetConversationCollectionAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult<HalCollectionResourceOfAiConversationSummaryDto?>(CreateConversationCollection([])),
                Task.FromResult<HalCollectionResourceOfAiConversationSummaryDto?>(
                    CreateConversationCollection(
                    [
                        new() { Id = conversationId, Title = "New AI plan", Status = "Active" }
                    ])));
        _clientService.CreateConversationAsync(Arg.Any<CreateAiConversationRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new AiAssistantCommandResult(true, conversationId, "Created", null, [])));
        _clientService.GetConversationAsync(conversationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalResourceOfAiConversationDto?>(CreateConversation(conversationId, "New AI plan")));
        _shellState.SetPolicy(tenantAvailable: true, allowAnonymousAccess: false, isAuthenticated: true);
        _shellState.Open();

        var cut = _ctx.RenderMudComponent<AiAssistantRail>();
        cut.WaitForElement("[data-testid='ai-rail-new-conversation']");
        await cut.Find("[data-testid='ai-rail-new-conversation']").ClickAsync(new MouseEventArgs());

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("New AI plan", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected created conversation to load into the rail.");
            }
        });
        await _clientService.Received(1).CreateConversationAsync(
            Arg.Is<CreateAiConversationRequestDto>(request => request.Title == "AI Assistant"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SendMessage_WhenConversationIsSelected_PropagatesGeneratedIdempotencyKeyAndReloadsConversation()
    {
        var conversationId = Guid.CreateVersion7();
        _conversationState.SelectConversation(CreateConversation(conversationId, "Send test"));
        _clientService.GetConversationCollectionAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalCollectionResourceOfAiConversationSummaryDto?>(CreateConversationCollection([])));
        _clientService.SendMessageAsync(conversationId, "Draft a khutbah event", Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new AiAssistantCommandResult(true, Guid.CreateVersion7(), "Sent", null, [])));
        _clientService.GetConversationAsync(conversationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalResourceOfAiConversationDto?>(CreateConversation(
                conversationId,
                "Send test",
                [
                    new Messages2 { Role = "Assistant", Sequence = 1, Content = "Draft created." }
                ])));
        _shellState.SetPolicy(tenantAvailable: true, allowAnonymousAccess: false, isAuthenticated: true);
        _shellState.Open();

        var cut = _ctx.RenderMudComponent<AiAssistantRail>();
        await cut.Find("[data-testid='ai-rail-prompt']").ChangeAsync(new ChangeEventArgs { Value = "Draft a khutbah event" });
        await cut.Find("[data-testid='ai-rail-send']").ClickAsync(new MouseEventArgs());

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Draft created.", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected sent message to reload conversation detail.");
            }
        });
        await _clientService.Received(1).SendMessageAsync(
            conversationId,
            "Draft a khutbah event",
            Arg.Is<string?>(value => value != null && value.StartsWith("blazor-ai-send-", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task NewConversation_WhenCreateFails_ShowsSafeErrorMessage()
    {
        _clientService.GetConversationCollectionAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalCollectionResourceOfAiConversationSummaryDto?>(CreateConversationCollection([])));
        _clientService.CreateConversationAsync(Arg.Any<CreateAiConversationRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new AiAssistantCommandResult(false, null, "AI assistant is disabled.", "disabled", [])));
        _shellState.SetPolicy(tenantAvailable: true, allowAnonymousAccess: false, isAuthenticated: true);
        _shellState.Open();

        var cut = _ctx.RenderMudComponent<AiAssistantRail>();
        cut.WaitForElement("[data-testid='ai-rail-new-conversation']");
        await cut.Find("[data-testid='ai-rail-new-conversation']").ClickAsync(new MouseEventArgs());

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("AI assistant is disabled.", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected safe create failure message.");
            }
        });
    }

    [Test]
    public async Task NewConversation_WhenCreateLinkMissing_HidesButtonAndDoesNotCallCreate()
    {
        _clientService.GetConversationCollectionAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalCollectionResourceOfAiConversationSummaryDto?>(
                CreateConversationCollection([], canCreate: false)));
        _shellState.SetPolicy(tenantAvailable: true, allowAnonymousAccess: false, isAuthenticated: true);
        _shellState.Open();

        var cut = _ctx.RenderMudComponent<AiAssistantRail>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Conversation creation is not available for your account.", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected missing HAL create link to render a safe empty state.");
            }
        });

        await Assert.That(cut.FindAll("[data-testid='ai-rail-new-conversation']")).IsEmpty();
        await _clientService.DidNotReceive().CreateConversationAsync(
            Arg.Any<CreateAiConversationRequestDto>(),
            Arg.Any<CancellationToken>());
    }

    private static HalCollectionResourceOfAiConversationSummaryDto CreateConversationCollection(
        IReadOnlyList<HalResourceOfAiConversationSummaryDto> conversations,
        bool canCreate = true)
    {
        return new HalCollectionResourceOfAiConversationSummaryDto
        {
            _links = canCreate
                ? new Dictionary<string, HalLink>
                {
                    ["create"] = new() { Href = "/api/ai/assistant/conversations", Method = "POST" }
                }
                : new Dictionary<string, HalLink>(),
            _embedded = new HalCollectionEmbeddedOfAiConversationSummaryDto
            {
                Items = conversations.ToList()
            }
        };
    }

    private static HalResourceOfAiConversationDto CreateConversation(
        Guid conversationId,
        string title,
        ICollection<Messages2>? messages = null)
    {
        return new HalResourceOfAiConversationDto
        {
            Id = conversationId,
            Title = title,
            Messages = messages,
            _links = new Dictionary<string, Anonymous6>
            {
                ["send-message"] = new() { Href = $"/api/ai/assistant/conversations/{conversationId}/messages", Method = "POST" }
            }
        };
    }
}
