// ABOUTME: bUnit coverage for the shell AI assistant rail UI.
// ABOUTME: Verifies generated-client service usage and HAL-gated proposed action affordances.

using Explore.Blazor.Client.Components.Shell;
using Explore.Blazor.Client.Contracts.Services.Ai;
using Explore.Blazor.Client.Services.Ai;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

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
        _shellState.SetPolicy(tenantEnabled: true, tenantAvailable: true, allowAnonymousAccess: false, isAuthenticated: true);
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
        _shellState.SetPolicy(tenantEnabled: true, tenantAvailable: true, allowAnonymousAccess: false, isAuthenticated: true);
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
    public async Task Render_WhenActionBelongsToAssistantMessage_RendersActionInlineAfterThatMessage()
    {
        var conversationId = Guid.CreateVersion7();
        var userMessageId = Guid.CreateVersion7();
        var assistantMessageId = Guid.CreateVersion7();
        _conversationState.SelectConversation(CreateConversation(
            conversationId,
            "Inline proposal",
            [
                new Messages2 { Id = userMessageId, Role = "User", Sequence = 1, Content = "Create an event draft" },
                new Messages2 { Id = assistantMessageId, Role = "Assistant", Sequence = 2, Content = "I prepared a draft for review." }
            ],
            [
                new ProposedActions2
                {
                    Id = Guid.CreateVersion7(),
                    MessageId = assistantMessageId,
                    Kind = "CreateEventDraft",
                    Status = "Proposed",
                    PayloadJson = "{\"title\":\"Community Iftar\",\"description\":\"Plan the meal.\"}",
                    _links = new Dictionary<string, Anonymous59>
                    {
                        ["confirm-action"] = new() { Href = "/confirm", Method = "POST" },
                        ["reject-action"] = new() { Href = "/reject", Method = "POST" }
                    }
                }
            ]));
        _clientService.GetConversationCollectionAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalCollectionResourceOfAiConversationSummaryDto?>(CreateConversationCollection([])));
        _shellState.SetPolicy(tenantEnabled: true, tenantAvailable: true, allowAnonymousAccess: false, isAuthenticated: true);
        _shellState.Open();

        var cut = _ctx.RenderMudComponent<AiAssistantRail>();

        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            var userIndex = markup.IndexOf("Create an event draft", StringComparison.Ordinal);
            var assistantIndex = markup.IndexOf("I prepared a draft for review.", StringComparison.Ordinal);
            var actionIndex = markup.IndexOf("Community Iftar", StringComparison.Ordinal);

            if (userIndex < 0 || assistantIndex < 0 || actionIndex < 0 || !(userIndex < assistantIndex && assistantIndex < actionIndex))
            {
                throw new InvalidOperationException("Expected proposal card to render inline after its assistant message.");
            }
        });
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
        _shellState.SetPolicy(tenantEnabled: true, tenantAvailable: true, allowAnonymousAccess: false, isAuthenticated: true);
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
        var actorId = Guid.CreateVersion7();
        var conversationId = Guid.CreateVersion7();
        _clientService.GetBootstrapAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalResourceOfAiAssistantBootstrapDto?>(CreateBootstrap(actorId, "User", "Amina Yusuf")));
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
        _shellState.SetPolicy(tenantEnabled: true, tenantAvailable: true, allowAnonymousAccess: false, isAuthenticated: true);
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
        await _clientService.Received(2).CreateConversationAsync(
            Arg.Is<CreateAiConversationRequestDto>(request => request.Title == "AI Assistant" && request.ActorId == actorId),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SendMessage_WhenConversationIsSelected_PropagatesGeneratedIdempotencyKeyAndReloadsConversation()
    {
        var actorId = Guid.CreateVersion7();
        var conversationId = Guid.CreateVersion7();
        _conversationState.SelectConversation(CreateConversation(conversationId, "Send test"));
        _clientService.GetBootstrapAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalResourceOfAiAssistantBootstrapDto?>(CreateBootstrap(actorId, "User", "Amina Yusuf")));
        _clientService.GetConversationCollectionAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalCollectionResourceOfAiConversationSummaryDto?>(CreateConversationCollection([])));
        _clientService.SendMessageAsync(
                conversationId,
                "Draft a khutbah event",
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<Guid?>(),
                Arg.Any<IReadOnlyList<AiMessageImageInputDto>?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new AiAssistantCommandResult(true, Guid.CreateVersion7(), "Sent", null, [])));
        _clientService.GetRunStatusAsync(conversationId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AiRunStatusResult.Ok(new HalResourceOfAiRunDto
            {
                Status = "Succeeded"
            })));
        _clientService.GetConversationAsync(conversationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalResourceOfAiConversationDto?>(CreateConversation(
                conversationId,
                "Send test",
                [
                    new Messages2 { Role = "Assistant", Sequence = 1, Content = "Draft created." }
                ])));
        _shellState.SetPolicy(tenantEnabled: true, tenantAvailable: true, allowAnonymousAccess: false, isAuthenticated: true);
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
            Arg.Any<string?>(),
            Arg.Is<string?>(value => value != null && value.StartsWith("blazor-ai-send-", StringComparison.Ordinal)),
            Arg.Is<string?>(value => value == "build"),
            Arg.Is<Guid?>(value => value == actorId),
            Arg.Is<IReadOnlyList<AiMessageImageInputDto>?>(value => value != null && value.Count == 0),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Render_AddFilesAction_UsesMudFileUploadPicker()
    {
        var conversationId = Guid.CreateVersion7();
        _conversationState.SelectConversation(CreateConversation(conversationId, "Image picker test"));
        _clientService.GetConversationCollectionAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalCollectionResourceOfAiConversationSummaryDto?>(CreateConversationCollection([])));
        _shellState.SetPolicy(tenantEnabled: true, tenantAvailable: true, allowAnonymousAccess: false, isAuthenticated: true);
        _shellState.Open();

        var cut = _ctx.RenderMudComponent<AiAssistantRail>();
        cut.WaitForElement("[data-testid='ai-rail-prompt']");

        await Assert.That(cut.FindComponents<MudFileUpload<IReadOnlyList<IBrowserFile>>>().Count).IsEqualTo(1);
        await Assert.That(cut.Markup).DoesNotContain("Browse files");
        await Assert.That(cut.Markup).DoesNotContain("mud-file-upload-files-default-template");
    }

    [Test]
    public async Task SendMessage_WhenImageIsSelected_ConvertsImageToPlainBase64()
    {
        var conversationId = Guid.CreateVersion7();
        var imageBytes = new byte[] { 1, 2, 3, 4 };
        var expectedBase64 = Convert.ToBase64String(imageBytes);
        _conversationState.SelectConversation(CreateConversation(conversationId, "Image send test"));
        _clientService.GetConversationCollectionAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalCollectionResourceOfAiConversationSummaryDto?>(CreateConversationCollection([])));
        _clientService.SendMessageAsync(
                conversationId,
                "Describe this picture:",
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<Guid?>(),
                Arg.Any<IReadOnlyList<AiMessageImageInputDto>?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new AiAssistantCommandResult(true, Guid.CreateVersion7(), "Sent", null, [])));
        _clientService.GetRunStatusAsync(conversationId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AiRunStatusResult.Ok(new HalResourceOfAiRunDto
            {
                Status = "Succeeded"
            })));
        _clientService.GetConversationAsync(conversationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalResourceOfAiConversationDto?>(CreateConversation(conversationId, "Image send test")));
        _shellState.SetPolicy(tenantEnabled: true, tenantAvailable: true, allowAnonymousAccess: false, isAuthenticated: true);
        _shellState.Open();

        var cut = _ctx.RenderMudComponent<AiAssistantRail>();
        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(
            imageBytes,
            "csharp.png",
            contentType: "image/png"));
        await cut.Find("[data-testid='ai-rail-prompt']").ChangeAsync(new ChangeEventArgs { Value = "Describe this picture:" });

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("csharp.png", StringComparison.Ordinal)
                || cut.FindAll("[data-testid='ai-rail-image-chip']").Count != 1
                || cut.Markup.Contains("Browse files", StringComparison.Ordinal)
                || cut.Markup.Contains("mud-file-upload-files-default-template", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected only the selected image chip list to render before sending.");
            }
        });

        await cut.Find("[data-testid='ai-rail-send']").ClickAsync(new MouseEventArgs());

        await _clientService.Received(1).SendMessageAsync(
            conversationId,
            "Describe this picture:",
            Arg.Any<string?>(),
            Arg.Is<string?>(value => value != null && value.StartsWith("blazor-ai-send-", StringComparison.Ordinal)),
            Arg.Is<string?>(value => value == "build"),
            Arg.Any<Guid?>(),
            Arg.Is<IReadOnlyList<AiMessageImageInputDto>?>(images =>
                images != null
                && images.Count == 1
                && images.Single().MediaType == "image/png"
                && images.Single().FileName == "csharp.png"
                && images.Single().SizeBytes == imageBytes.Length
                && images.Single().Data == expectedBase64
                && !images.Single().Data.StartsWith("data:", StringComparison.OrdinalIgnoreCase)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Render_WhenBootstrapHasActors_DefaultsActorSelectorToCurrentUser()
    {
        var userActorId = Guid.CreateVersion7();
        var organizationActorId = Guid.CreateVersion7();
        var conversationId = Guid.CreateVersion7();
        _clientService.GetBootstrapAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalResourceOfAiAssistantBootstrapDto?>(CreateBootstrap(
                (userActorId, "User", "Amina Yusuf"),
                (organizationActorId, "Organization", "ISLAMU Center"))));
        _conversationState.SelectConversation(CreateConversation(conversationId, "Actor test"));
        _clientService.GetConversationCollectionAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalCollectionResourceOfAiConversationSummaryDto?>(CreateConversationCollection([])));
        _shellState.SetPolicy(tenantEnabled: true, tenantAvailable: true, allowAnonymousAccess: false, isAuthenticated: true);
        _shellState.Open();

        var cut = _ctx.RenderMudComponent<AiAssistantRail>();

        var selector = cut.WaitForElement("[data-testid='ai-rail-actor-selector']");
        await Assert.That(selector.GetAttribute("value")).IsEqualTo(userActorId.ToString());
        await Assert.That(cut.Markup).Contains("User · Amina Yusuf");
        await Assert.That(cut.Markup).Contains("Organization · ISLAMU Center");
        await Assert.That(cut.Markup).Contains($"data-actor-type=\"User\"");
        await Assert.That(cut.Markup).Contains($"data-actor-display-name=\"Amina Yusuf\"");
    }

    [Test]
    public async Task SendMessage_WhenApiReturnsConflict_ReloadsConversationAndShowsProblemMessage()
    {
        var conversationId = Guid.CreateVersion7();
        _conversationState.SelectConversation(CreateConversation(conversationId, "Send conflict"));
        _clientService.GetConversationCollectionAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalCollectionResourceOfAiConversationSummaryDto?>(CreateConversationCollection([])));
        _clientService.SendMessageAsync(
                conversationId,
                "hello",
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<Guid?>(),
                Arg.Any<IReadOnlyList<AiMessageImageInputDto>?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new AiAssistantCommandResult(
                false,
                null,
                "AI conversation is not ready for a new message.",
                "conversation_not_active",
                ["AI conversation is not ready for a new message."])));
        _clientService.GetConversationAsync(conversationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalResourceOfAiConversationDto?>(CreateConversation(conversationId, "Send conflict")));
        _shellState.SetPolicy(tenantEnabled: true, tenantAvailable: true, allowAnonymousAccess: false, isAuthenticated: true);
        _shellState.Open();

        var cut = _ctx.RenderMudComponent<AiAssistantRail>();
        await cut.Find("[data-testid='ai-rail-prompt']").ChangeAsync(new ChangeEventArgs { Value = "hello" });
        await cut.Find("[data-testid='ai-rail-send']").ClickAsync(new MouseEventArgs());

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("AI conversation is not ready for a new message.", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected API problem detail to render in the rail.");
            }
        });
        await _clientService.Received(1).GetConversationAsync(conversationId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task NewConversation_WhenCreateFails_ShowsSafeErrorMessage()
    {
        _clientService.GetConversationCollectionAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalCollectionResourceOfAiConversationSummaryDto?>(CreateConversationCollection([])));
        _clientService.CreateConversationAsync(Arg.Any<CreateAiConversationRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new AiAssistantCommandResult(false, null, "AI assistant is disabled.", "disabled", [])));
        _shellState.SetPolicy(tenantEnabled: true, tenantAvailable: true, allowAnonymousAccess: false, isAuthenticated: true);
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
        _shellState.SetPolicy(tenantEnabled: true, tenantAvailable: true, allowAnonymousAccess: false, isAuthenticated: true);
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

    private static HalResourceOfAiAssistantBootstrapDto CreateBootstrap(Guid actorId, string actorType, string actorDisplayName)
    {
        return CreateBootstrap((actorId, actorType, actorDisplayName));
    }

    private static HalResourceOfAiAssistantBootstrapDto CreateBootstrap(params (Guid ActorId, string ActorType, string ActorDisplayName)[] actors)
    {
        return new HalResourceOfAiAssistantBootstrapDto
        {
            ActorContexts = actors.Select(actor => new ActorContexts2
            {
                ActorId = actor.ActorId,
                ActorType = actor.ActorType,
                ActorDisplayName = actor.ActorDisplayName
            }).ToList()
        };
    }

    private static HalResourceOfAiConversationDto CreateConversation(
        Guid conversationId,
        string title,
        ICollection<Messages2>? messages = null,
        ICollection<ProposedActions2>? proposedActions = null)
    {
        return new HalResourceOfAiConversationDto
        {
            Id = conversationId,
            Title = title,
            Messages = messages,
            ProposedActions = proposedActions,
            _links = new Dictionary<string, Anonymous6>
            {
                ["send-message"] = new() { Href = $"/api/ai/assistant/conversations/{conversationId}/messages", Method = "POST" }
            }
        };
    }
}
