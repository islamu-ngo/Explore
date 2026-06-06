// ABOUTME: bUnit coverage for the AI assistant reference picker component.
// ABOUTME: Verifies debounce search behavior and keyboard-removable selected chips.

using Explore.Blazor.Client.Components.Shell.AiAssistant;

namespace Explore.Blazor.Client.Tests.Components.Shell;

public sealed class AiReferencePickerTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();

    public void Dispose()
    {
        _ctx.Dispose();
    }

    [Test]
    public async Task SearchInput_DebouncesSearchRequests()
    {
        var searchedTerms = new List<string>();
        var cut = _ctx.RenderMudComponent<AiReferencePicker>(parameters => parameters
            .Add(component => component.SearchTerm, string.Empty)
            .Add(component => component.OnSearch, EventCallback.Factory.Create<string>(this, term => searchedTerms.Add(term))));

        await cut.Find("[data-testid='ai-rail-reference-search']").InputAsync(new ChangeEventArgs { Value = "if" });

        await WaitForAsync(
            () =>
            {
                if (!searchedTerms.Contains("if", StringComparer.Ordinal))
                {
                    throw new InvalidOperationException("Expected the debounced search callback to receive 'if'.");
                }
            },
            TimeSpan.FromSeconds(3));
    }

    [Test]
    public async Task SelectedChip_CanBeRemovedWithKeyboard()
    {
        var referenceId = Guid.CreateVersion7();
        Guid? removedReferenceId = null;
        var cut = _ctx.RenderMudComponent<AiReferencePicker>(parameters => parameters
            .Add(component => component.SelectedReferences, [new HalResourceOfAiReferenceSearchResultDto
            {
                ReferenceId = referenceId,
                DisplayName = "Community Iftar",
                Kind = "Event"
            }])
            .Add(component => component.OnRemove, EventCallback.Factory.Create<Guid>(this, id => removedReferenceId = id)));

        await cut.Find("[data-testid='ai-reference-chip']").KeyDownAsync(new KeyboardEventArgs { Key = "Delete" });

        await Assert.That(removedReferenceId).IsEqualTo(referenceId);
    }

    private static async Task WaitForAsync(Action assertion, TimeSpan? timeout = null)
    {
        var deadline = DateTimeOffset.UtcNow + (timeout ?? TimeSpan.FromSeconds(3));
        Exception? lastException = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                assertion();
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                await Task.Delay(TimeSpan.FromMilliseconds(50));
            }
        }

        try
        {
            assertion();
        }
        catch (Exception ex)
        {
            throw new TimeoutException("The expected assertion did not pass before the timeout.", lastException ?? ex);
        }
    }
}
