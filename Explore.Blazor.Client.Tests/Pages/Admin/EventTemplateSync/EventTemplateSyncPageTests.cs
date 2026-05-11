// ABOUTME: Component tests for EventTemplateSyncPage.
// ABOUTME: Covers HAL-gated render, 409 handling, and slug confirmation tests.

using System.Net;
using Explore.Blazor.Client.Components.EventTemplateSync;
using Explore.Blazor.Client.Models.EventTemplateSync;
using EventTemplateSyncApplyRequest = Explore.Blazor.Client.Models.EventTemplateSync.EventTemplateSyncApplyRequest;
using Explore.Blazor.Client.Pages.Admin.EventTemplateSync;
using Explore.Blazor.Client.Services.EventTemplateSync;
using MudBlazor;
using AddedDefinitionDto = Explore.Blazor.Client.Models.EventTemplateSync.AddedDefinitionDto;
using AddedOptionDto = Explore.Blazor.Client.Models.EventTemplateSync.AddedOptionDto;
using FieldChangeDto = Explore.Blazor.Client.Models.EventTemplateSync.FieldChangeDto;
using ModifiedDefinitionDto = Explore.Blazor.Client.Models.EventTemplateSync.ModifiedDefinitionDto;
using ModifiedOptionDto = Explore.Blazor.Client.Models.EventTemplateSync.ModifiedOptionDto;
using RetiredDefinitionDto = Explore.Blazor.Client.Models.EventTemplateSync.RetiredDefinitionDto;
using RetiredOptionDto = Explore.Blazor.Client.Models.EventTemplateSync.RetiredOptionDto;
using TemplateDiffDto = Explore.Blazor.Client.Models.EventTemplateSync.TemplateDiffDto;
using UntouchedLocalDefinitionDto = Explore.Blazor.Client.Models.EventTemplateSync.UntouchedLocalDefinitionDto;

namespace Explore.Blazor.Client.Tests.Pages.Admin.EventTemplateSync;

public sealed class EventTemplateSyncPageTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IEventTemplateSyncService _templateSyncService;
    private readonly ISnackbar _snackbar;
    private readonly IDialogService _dialogService;

    public EventTemplateSyncPageTests()
    {
        _ctx = new BlazorTestContext();
        _templateSyncService = Substitute.For<IEventTemplateSyncService>();
        _snackbar = Substitute.For<ISnackbar>();
        _dialogService = Substitute.For<IDialogService>();

        _ctx.Services.AddSingleton(_templateSyncService);
        _ctx.Services.AddSingleton(_snackbar);
        _ctx.Services.AddSingleton(_dialogService);

        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Admin User", "admin@example.com");
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    private IRenderedComponent<EventTemplateSyncPage> RenderPage(Guid eventId, int? version = null)
    {
        return _ctx.RenderMudComponent<EventTemplateSyncPage>(p =>
        {
            p.Add(x => x.EventId, eventId);
            if (version.HasValue)
            {
                p.Add(x => x.TemplateVersion, version.Value);
            }
        });
    }

    [Test]
    public async Task EventTemplateSyncPage_DiffFetchAndRender_ShowsCountsAndLocalChangesBanner()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var diff = new TemplateDiffDto(
            TargetTemplateVersion: 2,
            BaseProvenanceVersion: 1,
            AddedDefinitions: Array.Empty<AddedDefinitionDto>(),
            ModifiedDefinitions: [
                new ModifiedDefinitionDto("Test", "Field", Guid.Empty, new[] { new FieldChangeDto("Name", "Old", "New", "string") })
            ],
            RetiredDefinitions: Array.Empty<RetiredDefinitionDto>(),
            AddedOptions: Array.Empty<AddedOptionDto>(),
            ModifiedOptions: Array.Empty<ModifiedOptionDto>(),
            RetiredOptions: Array.Empty<RetiredOptionDto>(),
            UntouchedLocalDefinitions: [
                new UntouchedLocalDefinitionDto("Local", "Item", "Test")
            ])
        {
            Links = new Dictionary<string, Explore.Blazor.Client.Models.EventTemplateSync.HalLinkDto>()
        };

        _templateSyncService.GetDiffAsync(eventId, 0).Returns(diff);

        // Act
        var cut = RenderPage(eventId);
        cut.WaitForState(() => !cut.Markup.Contains("mud-progress-circular"), TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).Contains("Warning: Event has 1 untouched local definitions");
        await Assert.That(cut.Markup).Contains("Modified (1)");
        await Assert.That(cut.Markup).Contains("Test.Field");
    }

    [Test]
    public async Task EventTemplateSyncPage_HalLinkAbsence_ApplyButtonHidden()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var diff = new TemplateDiffDto(
            TargetTemplateVersion: 2,
            BaseProvenanceVersion: 1,
            AddedDefinitions: Array.Empty<AddedDefinitionDto>(),
            ModifiedDefinitions: [
                new ModifiedDefinitionDto("Test", "Field", Guid.Empty, new[] { new FieldChangeDto("Name", "Old", "New", "string") })
            ],
            RetiredDefinitions: Array.Empty<RetiredDefinitionDto>(),
            AddedOptions: Array.Empty<AddedOptionDto>(),
            ModifiedOptions: Array.Empty<ModifiedOptionDto>(),
            RetiredOptions: Array.Empty<RetiredOptionDto>(),
            UntouchedLocalDefinitions: Array.Empty<UntouchedLocalDefinitionDto>())
        {
            Links = new Dictionary<string, Explore.Blazor.Client.Models.EventTemplateSync.HalLinkDto>() // No sync-apply link
        };

        _templateSyncService.GetDiffAsync(eventId, 0).Returns(diff);

        // Act
        var cut = RenderPage(eventId);
        cut.WaitForState(() => !cut.Markup.Contains("mud-progress-circular"), TimeSpan.FromSeconds(3));

        var row = cut.FindComponent<TemplateDiffRow>();
        row.Find("input[type=\"checkbox\"]").Change(true);

        // Assert
        await Assert.That(cut.Markup).DoesNotContain("Apply Sync (1 changes)");
    }

    [Test]
    public async Task EventTemplateSyncPage_HalLinkPresent_ApplyButtonVisibleAndEnabledOnSelection()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var diff = new TemplateDiffDto(
            TargetTemplateVersion: 2,
            BaseProvenanceVersion: 1,
            AddedDefinitions: Array.Empty<AddedDefinitionDto>(),
            ModifiedDefinitions: [
                new ModifiedDefinitionDto("Test", "Field", Guid.Empty, new[] { new FieldChangeDto("Name", "Old", "New", "string") })
            ],
            RetiredDefinitions: Array.Empty<RetiredDefinitionDto>(),
            AddedOptions: Array.Empty<AddedOptionDto>(),
            ModifiedOptions: Array.Empty<ModifiedOptionDto>(),
            RetiredOptions: Array.Empty<RetiredOptionDto>(),
            UntouchedLocalDefinitions: Array.Empty<UntouchedLocalDefinitionDto>())
        {
            Links = new Dictionary<string, Explore.Blazor.Client.Models.EventTemplateSync.HalLinkDto>
            {
                ["sync-apply"] = new Explore.Blazor.Client.Models.EventTemplateSync.HalLinkDto { Href = "/apply" }
            }
        };

        _templateSyncService.GetDiffAsync(eventId, 0).Returns(diff);

        // Act
        var cut = RenderPage(eventId);
        cut.WaitForState(() => !cut.Markup.Contains("mud-progress-circular"), TimeSpan.FromSeconds(3));

        await Assert.That(cut.Markup).DoesNotContain("Apply Sync");

        var row = cut.FindComponent<TemplateDiffRow>();
        row.Find("input[type=\"checkbox\"]").Change(true);
        cut.WaitForState(() => cut.Markup.Contains("Apply Sync (1 changes)"));

        // Assert
        await Assert.That(cut.Markup).Contains("Apply Sync (1 changes)");
    }

    [Test]
    public async Task EventTemplateSyncPage_SlugConfirmationEnforcement_CallsApplyOnSuccess()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var diff = new TemplateDiffDto(
            TargetTemplateVersion: 2,
            BaseProvenanceVersion: 1,
            AddedDefinitions: Array.Empty<AddedDefinitionDto>(),
            ModifiedDefinitions: [
                new ModifiedDefinitionDto("Test", "Field", Guid.Empty, new[] { new FieldChangeDto("Name", "Old", "New", "string") })
            ],
            RetiredDefinitions: Array.Empty<RetiredDefinitionDto>(),
            AddedOptions: Array.Empty<AddedOptionDto>(),
            ModifiedOptions: Array.Empty<ModifiedOptionDto>(),
            RetiredOptions: Array.Empty<RetiredOptionDto>(),
            UntouchedLocalDefinitions: Array.Empty<UntouchedLocalDefinitionDto>())
        {
            Links = new Dictionary<string, Explore.Blazor.Client.Models.EventTemplateSync.HalLinkDto> { ["sync-apply"] = new Explore.Blazor.Client.Models.EventTemplateSync.HalLinkDto { Href = "/apply" } }
        };

        _templateSyncService.GetDiffAsync(eventId, 0).Returns(diff);

        var dialogReference = Substitute.For<IDialogReference>();
        dialogReference.Result.Returns(DialogResult.Ok(true));
        _dialogService.ShowAsync<TemplateSyncConfirmationDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(dialogReference);

        var cut = RenderPage(eventId);
        cut.WaitForState(() => !cut.Markup.Contains("mud-progress-circular"), TimeSpan.FromSeconds(3));

        var row = cut.FindComponent<TemplateDiffRow>();
        row.Find("input[type=\"checkbox\"]").Change(true);
        cut.WaitForState(() => cut.Markup.Contains("Apply Sync (1 changes)"));

        // Act
        cut.Find("button.template-sync-page__apply-button").Click();

        // Assert
        await _dialogService.Received(1).ShowAsync<TemplateSyncConfirmationDialog>("Confirm Sync", Arg.Is<DialogParameters>(p =>
            p.Get<string>("ExpectedSlug") == "event-slug" &&
            p.Get<int>("TargetTemplateVersion") == 2 &&
            p.Get<int>("TotalChangesToApply") == 1
        ), Arg.Any<DialogOptions>());

        await _templateSyncService.Received(1).ApplySyncAsync(eventId, Arg.Is<EventTemplateSyncApplyRequest>(r =>
            r.Plan.TargetTemplateVersion == 2 &&
            r.Plan.BaseProvenanceVersion == 1 &&
            r.Plan.ModifiedDefinitionKeys.Contains("Field")
        ));
    }

    [Test]
    public async Task EventTemplateSyncPage_409StaleSyncBase_ShowsReFetchBanner()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _templateSyncService.GetDiffAsync(eventId, 0).Throws(new HttpRequestException("Conflict", null, HttpStatusCode.Conflict));

        // Act
        var cut = RenderPage(eventId);
        cut.WaitForState(() => !cut.Markup.Contains("mud-progress-circular"), TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).Contains("Template has been modified by another operator.");
        await Assert.That(cut.Markup).Contains("Reload Diff");
    }

    [Test]
    public async Task EventTemplateSyncPage_409ConcurrentUpdateOnApply_ShowsReFetchBanner()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var diff = new TemplateDiffDto(
            TargetTemplateVersion: 2,
            BaseProvenanceVersion: 1,
            AddedDefinitions: Array.Empty<AddedDefinitionDto>(),
            ModifiedDefinitions: [
                new ModifiedDefinitionDto("Test", "Field", Guid.Empty, new[] { new FieldChangeDto("Name", "Old", "New", "string") })
            ],
            RetiredDefinitions: Array.Empty<RetiredDefinitionDto>(),
            AddedOptions: Array.Empty<AddedOptionDto>(),
            ModifiedOptions: Array.Empty<ModifiedOptionDto>(),
            RetiredOptions: Array.Empty<RetiredOptionDto>(),
            UntouchedLocalDefinitions: Array.Empty<UntouchedLocalDefinitionDto>())
        {
            Links = new Dictionary<string, Explore.Blazor.Client.Models.EventTemplateSync.HalLinkDto> { ["sync-apply"] = new Explore.Blazor.Client.Models.EventTemplateSync.HalLinkDto { Href = "/apply" } }
        };

        _templateSyncService.GetDiffAsync(eventId, 0).Returns(diff);

        var dialogReference = Substitute.For<IDialogReference>();
        dialogReference.Result.Returns(DialogResult.Ok(true));
        _dialogService.ShowAsync<TemplateSyncConfirmationDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(dialogReference);

        _templateSyncService.ApplySyncAsync(eventId, Arg.Any<EventTemplateSyncApplyRequest>())
            .Throws(new HttpRequestException("Conflict", null, HttpStatusCode.Conflict));

        var cut = RenderPage(eventId);
        cut.WaitForState(() => !cut.Markup.Contains("mud-progress-circular"), TimeSpan.FromSeconds(3));

        var row = cut.FindComponent<TemplateDiffRow>();
        row.Find("input[type=\"checkbox\"]").Change(true);
        cut.WaitForState(() => cut.Markup.Contains("Apply Sync (1 changes)"));

        // Act
        cut.Find("button.template-sync-page__apply-button").Click();
        cut.WaitForState(() => cut.Markup.Contains("Template has been modified by another operator."), TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).Contains("Template has been modified by another operator.");
    }
}
