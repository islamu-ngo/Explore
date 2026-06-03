// ABOUTME: bUnit coverage for HAL-gated AI proposed action cards.
// ABOUTME: Verifies preview/result rendering and absence of local authorization decisions.

using Explore.Blazor.Client.Components.Shell.AiAssistant;

namespace Explore.Blazor.Client.Tests.Components.Shell;

public sealed class AiProposedActionCardTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();

    public void Dispose()
    {
        _ctx.Dispose();
    }

    [Test]
    public async Task Render_WhenHalLinksExist_ShowsConfirmRejectAndSafePreview()
    {
        var action = new ProposedActions2
        {
            Id = Guid.CreateVersion7(),
            Kind = "CreateEventDraft",
            Status = "Proposed",
            PayloadJson = "{\"title\":\"Community Iftar\",\"description\":\"Plan the meal.\",\"tenantId\":\"not-rendered\"}",
            _links = new Dictionary<string, Anonymous59>
            {
                ["confirm-action"] = new() { Href = "/confirm", Method = "POST" },
                ["reject-action"] = new() { Href = "/reject", Method = "POST" }
            }
        };
        ProposedActions2? confirmed = null;

        var cut = _ctx.RenderMudComponent<AiProposedActionCard>(parameters => parameters
            .Add(component => component.Action, action)
            .Add(component => component.OnConfirm, EventCallback.Factory.Create<ProposedActions2>(this, value => confirmed = value)));

        await Assert.That(cut.FindAll("[data-testid='ai-rail-confirm-action']").Count).IsEqualTo(1);
        await Assert.That(cut.FindAll("[data-testid='ai-rail-reject-action']").Count).IsEqualTo(1);
        await Assert.That(cut.Markup).Contains("Community Iftar");
        await Assert.That(cut.Markup).Contains("Plan the meal.");
        await Assert.That(cut.Markup).DoesNotContain("not-rendered");

        await cut.Find("[data-testid='ai-rail-confirm-action']").ClickAsync(new MouseEventArgs());

        await Assert.That(confirmed).IsSameReferenceAs(action);
    }

    [Test]
    public async Task Render_WhenHalLinksAreAbsent_HidesConfirmReject()
    {
        var action = new ProposedActions2
        {
            Id = Guid.CreateVersion7(),
            Kind = "CreateEventDraft",
            Status = "Executed",
            ResultResourceId = Guid.CreateVersion7()
        };

        var cut = _ctx.RenderMudComponent<AiProposedActionCard>(parameters => parameters
            .Add(component => component.Action, action));

        await Assert.That(cut.FindAll("[data-testid='ai-rail-confirm-action']")).IsEmpty();
        await Assert.That(cut.FindAll("[data-testid='ai-rail-reject-action']")).IsEmpty();
        await Assert.That(cut.FindAll("[data-testid='ai-action-result']").Count).IsEqualTo(1);
    }
}
