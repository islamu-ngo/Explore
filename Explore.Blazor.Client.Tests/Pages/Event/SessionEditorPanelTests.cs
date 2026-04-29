// ABOUTME: Component tests for SessionEditorPanel session blueprint selection.
// ABOUTME: Verifies scoped loading, stale async guards, failure clearing, and submit blocking.

using Explore.Blazor.Client.Pages.Events.Components;
using Explore.Blazor.Client.Pages.Events.Models;
using System.Reflection;

namespace Explore.Blazor.Client.Tests.Pages.Event;

public sealed class SessionEditorPanelTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IEventSessionTemplateService _sessionTemplateService;

    public SessionEditorPanelTests()
    {
        _ctx = new BlazorTestContext();
        _sessionTemplateService = Substitute.For<IEventSessionTemplateService>();

        _ctx.Services.AddSingleton(Substitute.For<IImageStorageService>());
        _ctx.Services.AddSingleton(_sessionTemplateService);
        _ctx.Services.AddSingleton(Substitute.For<ILogger<SessionEditorPanel>>());

        _sessionTemplateService.GetTemplatesAsync(
                Arg.Any<Guid?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(PaginatedResult<EventSessionTemplateListModel>.Empty(pageSize: 100));
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    [Test]
    public async Task SessionEditorPanel_WhenNewSessionWithParentTemplate_LoadsScopedBlueprints()
    {
        var parentTemplateId = Guid.NewGuid();
        var template = CreateSessionTemplateListModel(Guid.NewGuid(), parentTemplateId, "Lecture Blueprint");
        _sessionTemplateService.GetTemplatesAsync(parentTemplateId, 1, 100, Arg.Any<CancellationToken>())
            .Returns(CreateSessionTemplatePage(template));

        var cut = RenderPanel(parentTemplateId: parentTemplateId);

        cut.WaitForAssertion(() =>
            _sessionTemplateService.Received(1).GetTemplatesAsync(parentTemplateId, 1, 100, Arg.Any<CancellationToken>()),
            TimeSpan.FromSeconds(3));

        var templates = GetPrivateField<List<EventSessionTemplateListModel>>(cut.Instance, "_sessionTemplates");
        await Assert.That(templates.Count).IsEqualTo(1);
        await Assert.That(templates[0].Id).IsEqualTo(template.Id);
    }

    [Test]
    public async Task SessionEditorPanel_WhenParentTemplateChanges_ClearsSelectionAndReloadsScopedBlueprints()
    {
        var firstParentId = Guid.NewGuid();
        var secondParentId = Guid.NewGuid();
        var secondTemplate = CreateSessionTemplateListModel(Guid.NewGuid(), secondParentId, "Workshop Blueprint");
        _sessionTemplateService.GetTemplatesAsync(secondParentId, 1, 100, Arg.Any<CancellationToken>())
            .Returns(CreateSessionTemplatePage(secondTemplate));

        var session = new SessionEditorModel { SessionTemplateId = Guid.NewGuid() };
        var cut = RenderPanel(session, firstParentId);
        SetPrivateField(cut.Instance, "_selectedSessionTemplate", CreateSessionTemplateDetailModel(session.SessionTemplateId.Value, firstParentId, "Old Blueprint"));

        cut.Instance.Session = session;
        cut.Instance.IsNew = true;
        cut.Instance.ParentEventTemplateId = secondParentId;
        await InvokePrivateAsync(cut.Instance, "OnParametersSetAsync");

        cut.WaitForAssertion(() =>
            _sessionTemplateService.Received(1).GetTemplatesAsync(secondParentId, 1, 100, Arg.Any<CancellationToken>()),
            TimeSpan.FromSeconds(3));

        var model = GetPrivateField<SessionEditorModel>(cut.Instance, "_model");
        var selectedTemplate = GetPrivateField<EventSessionTemplateDetailModel?>(cut.Instance, "_selectedSessionTemplate");
        var templates = GetPrivateField<List<EventSessionTemplateListModel>>(cut.Instance, "_sessionTemplates");

        await Assert.That(model.SessionTemplateId).IsNull();
        await Assert.That(selectedTemplate).IsNull();
        await Assert.That(templates.Count).IsEqualTo(1);
        await Assert.That(templates[0].Id).IsEqualTo(secondTemplate.Id);
    }

    [Test]
    public async Task SessionEditorPanel_WhenBlueprintPreviewFails_ClearsSessionTemplateId()
    {
        var parentTemplateId = Guid.NewGuid();
        var missingTemplateId = Guid.NewGuid();
        _sessionTemplateService.GetTemplateByIdAsync(missingTemplateId, Arg.Any<CancellationToken>())
            .Returns((EventSessionTemplateDetailModel?)null);

        var cut = RenderPanel(parentTemplateId: parentTemplateId);

        await InvokePrivateAsync(cut.Instance, "OnSessionTemplateChanged", missingTemplateId);

        var model = GetPrivateField<SessionEditorModel>(cut.Instance, "_model");
        var error = GetPrivateField<string?>(cut.Instance, "_sessionTemplateLoadError");

        await Assert.That(model.SessionTemplateId).IsNull();
        await Assert.That(error).Contains("selection was cleared");
    }

    [Test]
    public async Task SessionEditorPanel_WhenBlueprintPreviewRequestsRace_KeepsLatestSelectionOnly()
    {
        var parentTemplateId = Guid.NewGuid();
        var slowTemplateId = Guid.NewGuid();
        var fastTemplateId = Guid.NewGuid();
        var slowPreview = new TaskCompletionSource<EventSessionTemplateDetailModel?>(TaskCreationOptions.RunContinuationsAsynchronously);

        _sessionTemplateService.GetTemplateByIdAsync(slowTemplateId, Arg.Any<CancellationToken>())
            .Returns(slowPreview.Task);
        _sessionTemplateService.GetTemplateByIdAsync(fastTemplateId, Arg.Any<CancellationToken>())
            .Returns(CreateSessionTemplateDetailModel(fastTemplateId, parentTemplateId, "Fast Blueprint"));

        var cut = RenderPanel(parentTemplateId: parentTemplateId);

        var slowRequest = InvokePrivateAsync(cut.Instance, "OnSessionTemplateChanged", slowTemplateId);
        await InvokePrivateAsync(cut.Instance, "OnSessionTemplateChanged", fastTemplateId);
        slowPreview.SetResult(CreateSessionTemplateDetailModel(slowTemplateId, parentTemplateId, "Slow Blueprint"));
        await slowRequest;

        var model = GetPrivateField<SessionEditorModel>(cut.Instance, "_model");
        var selectedTemplate = GetPrivateField<EventSessionTemplateDetailModel?>(cut.Instance, "_selectedSessionTemplate");

        await Assert.That(model.SessionTemplateId).IsEqualTo(fastTemplateId);
        await Assert.That(selectedTemplate).IsNotNull();
        await Assert.That(selectedTemplate!.Id).IsEqualTo(fastTemplateId);
    }

    [Test]
    public async Task SessionEditorPanel_WhenBlueprintListRequestsRace_KeepsLatestParentOnly()
    {
        var firstParentId = Guid.NewGuid();
        var secondParentId = Guid.NewGuid();
        var firstTemplate = CreateSessionTemplateListModel(Guid.NewGuid(), firstParentId, "First Blueprint");
        var secondTemplate = CreateSessionTemplateListModel(Guid.NewGuid(), secondParentId, "Second Blueprint");
        var slowList = new TaskCompletionSource<PaginatedResult<EventSessionTemplateListModel>>(TaskCreationOptions.RunContinuationsAsynchronously);

        _sessionTemplateService.GetTemplatesAsync(firstParentId, 1, 100, Arg.Any<CancellationToken>())
            .Returns(slowList.Task);
        _sessionTemplateService.GetTemplatesAsync(secondParentId, 1, 100, Arg.Any<CancellationToken>())
            .Returns(CreateSessionTemplatePage(secondTemplate));

        var cut = RenderPanel(parentTemplateId: firstParentId);
        cut.Instance.Session = new SessionEditorModel();
        cut.Instance.IsNew = true;
        cut.Instance.ParentEventTemplateId = secondParentId;
        await InvokePrivateAsync(cut.Instance, "OnParametersSetAsync");

        slowList.SetResult(CreateSessionTemplatePage(firstTemplate));
        await Task.Yield();

        var templates = GetPrivateField<List<EventSessionTemplateListModel>>(cut.Instance, "_sessionTemplates");

        await Assert.That(templates.Count).IsEqualTo(1);
        await Assert.That(templates[0].Id).IsEqualTo(secondTemplate.Id);
    }

    [Test]
    public async Task SessionEditorPanel_WhenBlueprintPreviewIsLoading_DoesNotSave()
    {
        var parentTemplateId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var preview = new TaskCompletionSource<EventSessionTemplateDetailModel?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var saved = false;

        _sessionTemplateService.GetTemplateByIdAsync(templateId, Arg.Any<CancellationToken>())
            .Returns(preview.Task);

        var cut = RenderPanel(
            parentTemplateId: parentTemplateId,
            onSave: _ =>
            {
                saved = true;
                return Task.CompletedTask;
            });

        var previewRequest = InvokePrivateAsync(cut.Instance, "OnSessionTemplateChanged", templateId);

        await InvokePrivateAsync(cut.Instance, "Submit");

        var error = GetPrivateField<string?>(cut.Instance, "_sessionTemplateLoadError");
        await Assert.That(saved).IsFalse();
        await Assert.That(error).Contains("preview");

        preview.SetResult(CreateSessionTemplateDetailModel(templateId, parentTemplateId, "Blueprint"));
        await previewRequest;
    }

    private IRenderedComponent<SessionEditorPanel> RenderPanel(
        SessionEditorModel? session = null,
        Guid? parentTemplateId = null,
        Func<SessionEditorModel, Task>? onSave = null)
    {
        return _ctx.RenderMudComponent<SessionEditorPanel>(parameters => parameters
            .Add(component => component.Session, session ?? new SessionEditorModel())
            .Add(component => component.IsNew, true)
            .Add(component => component.ParentEventTemplateId, parentTemplateId)
            .Add(component => component.OnSave, EventCallback.Factory.Create<SessionEditorModel>(this, onSave ?? (_ => Task.CompletedTask)))
            .Add(component => component.OnCancel, EventCallback.Factory.Create(this, () => Task.CompletedTask)));
    }

    private static PaginatedResult<EventSessionTemplateListModel> CreateSessionTemplatePage(params EventSessionTemplateListModel[] templates) => new()
    {
        Items = templates.ToList(),
        PageNumber = 1,
        PageSize = 100,
        TotalCount = templates.Length
    };

    private static EventSessionTemplateListModel CreateSessionTemplateListModel(Guid id, Guid parentTemplateId, string displayName) => new()
    {
        Id = id,
        EventTemplateId = parentTemplateId,
        TenantId = Guid.NewGuid(),
        SessionTemplateKey = displayName.ToLowerInvariant().Replace(' ', '-'),
        DisplayName = displayName,
        Version = 1,
        IsActive = true,
        IsPublished = true,
        DefinitionCount = 1
    };

    private static EventSessionTemplateDetailModel CreateSessionTemplateDetailModel(Guid id, Guid parentTemplateId, string displayName) => new()
    {
        Id = id,
        EventTemplateId = parentTemplateId,
        TenantId = Guid.NewGuid(),
        SessionTemplateKey = displayName.ToLowerInvariant().Replace(' ', '-'),
        DisplayName = displayName,
        Version = 1,
        IsActive = true,
        IsPublished = true,
        Definitions = new List<EventSessionTemplateDefinitionModel>
        {
            new()
            {
                Key = "topic",
                DisplayName = "Topic",
                SortOrder = 1
            }
        }
    };

    private static async Task InvokePrivateAsync(SessionEditorPanel component, string methodName, params object?[] args)
    {
        var method = typeof(SessionEditorPanel).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(typeof(SessionEditorPanel).FullName, methodName);

        var result = method.Invoke(component, args);
        if (result is Task task)
        {
            await task;
            return;
        }

        throw new InvalidOperationException($"Private method {methodName} did not return a Task.");
    }

    private static T GetPrivateField<T>(SessionEditorPanel component, string fieldName)
    {
        var field = typeof(SessionEditorPanel).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(SessionEditorPanel).FullName, fieldName);

        return (T)field.GetValue(component)!;
    }

    private static void SetPrivateField<T>(SessionEditorPanel component, string fieldName, T value)
    {
        var field = typeof(SessionEditorPanel).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(SessionEditorPanel).FullName, fieldName);

        field.SetValue(component, value);
    }
}
