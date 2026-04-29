// ABOUTME: Unit tests for the shared session editor workflow used by CreateEvent and EventEdit.
// ABOUTME: Verifies drawer state, navigation, save, duplication, and default-session creation behavior.

using Explore.Blazor.Client.Pages.Events.Models;
using Explore.Blazor.Client.Pages.Events.Workflows;

namespace Explore.Blazor.Client.Tests.Pages.Events.Workflows;

public class SessionEditorWorkflowTests
{
    [Test]
    public async Task CreateDefaultSession_InheritsEventContext()
    {
        var workflow = new SessionEditorWorkflow();
        var sessions = new List<SessionEditorModel>
        {
            new()
            {
                StartTime = new DateTime(2025, 6, 1, 9, 0, 0),
                EndTime = new DateTime(2025, 6, 1, 17, 0, 0),
                RegistrationModeId = 3,
                LocationId = Guid.NewGuid()
            }
        };

        var session = workflow.CreateDefaultSession(sessions, "https://example.com/event.jpg");

        await Assert.That(session.StartTime).IsEqualTo(sessions[0].StartTime.AddDays(1));
        await Assert.That(session.EndTime).IsEqualTo(sessions[0].EndTime.AddDays(1));
        await Assert.That(session.RegistrationModeId).IsEqualTo(3);
        await Assert.That(session.LocationId).IsEqualTo(sessions[0].LocationId);
        await Assert.That(session.UseEventImage).IsTrue();
        await Assert.That(session.FeaturedImagePreviewUrl).IsEqualTo("https://example.com/event.jpg");
    }

    [Test]
    public async Task OpenForEdit_CopiesSessionIntoDrawerState()
    {
        var workflow = new SessionEditorWorkflow();
        var sessions = new List<SessionEditorModel>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Session 1",
                SessionTemplateId = Guid.NewGuid(),
                LanguageIds = new HashSet<int> { 1, 2 }
            }
        };

        workflow.OpenForEdit(sessions, 0);

        await Assert.That(workflow.IsDrawerOpen).IsTrue();
        await Assert.That(workflow.IsNewSession).IsFalse();
        await Assert.That(workflow.EditingSessionIndex).IsEqualTo(0);
        await Assert.That(workflow.DrawerModel).IsNotNull();
        await Assert.That(workflow.DrawerModel!.Title).IsEqualTo("Session 1");
        await Assert.That(workflow.DrawerModel.SessionTemplateId).IsEqualTo(sessions[0].SessionTemplateId);
        await Assert.That(ReferenceEquals(workflow.DrawerModel, sessions[0])).IsFalse();
    }

    [Test]
    public async Task SaveSession_ForNewSession_AppendsAndClosesDrawer()
    {
        var workflow = new SessionEditorWorkflow();
        var sessions = new List<SessionEditorModel>();
        workflow.OpenForCreate(sessions, null);

        var session = new SessionEditorModel { Title = "New Session" };

        workflow.SaveSession(sessions, session);

        await Assert.That(sessions.Count).IsEqualTo(1);
        await Assert.That(sessions[0].Title).IsEqualTo("New Session");
        await Assert.That(workflow.IsDrawerOpen).IsFalse();
        await Assert.That(workflow.DrawerModel).IsNull();
    }

    [Test]
    public async Task NavigateNext_PersistsCurrentDrawerModel_AndMovesToFollowingSession()
    {
        var workflow = new SessionEditorWorkflow();
        var sessions = new List<SessionEditorModel>
        {
            new() { Title = "One" },
            new() { Title = "Two" }
        };

        workflow.OpenForEdit(sessions, 0);
        workflow.DrawerModel!.Title = "One Updated";

        workflow.NavigateNext(sessions, null);

        await Assert.That(sessions[0].Title).IsEqualTo("One Updated");
        await Assert.That(workflow.EditingSessionIndex).IsEqualTo(1);
        await Assert.That(workflow.DrawerModel!.Title).IsEqualTo("Two");
    }

    [Test]
    public async Task AddFromDrawer_SavesCurrentNewSession_AndOpensFreshDefaultSession()
    {
        var workflow = new SessionEditorWorkflow();
        var sessions = new List<SessionEditorModel>();
        workflow.OpenForCreate(sessions, "https://example.com/event.jpg");
        workflow.DrawerModel!.Title = "First New";

        workflow.AddFromDrawer(sessions, "https://example.com/event.jpg");

        await Assert.That(sessions.Count).IsEqualTo(1);
        await Assert.That(sessions[0].Title).IsEqualTo("First New");
        await Assert.That(workflow.IsDrawerOpen).IsTrue();
        await Assert.That(workflow.IsNewSession).IsTrue();
        await Assert.That(workflow.DrawerModel).IsNotNull();
        await Assert.That(workflow.DrawerModel!.Title).IsNull();
    }

    [Test]
    public async Task DuplicateSession_AppendsClone()
    {
        var workflow = new SessionEditorWorkflow();
        var sessions = new List<SessionEditorModel>
        {
            new()
            {
                Title = "Original",
                StartTime = new DateTime(2025, 6, 1, 9, 0, 0),
                EndTime = new DateTime(2025, 6, 1, 10, 0, 0)
            }
        };

        var duplicated = workflow.DuplicateSession(sessions, 0);

        await Assert.That(duplicated).IsTrue();
        await Assert.That(sessions.Count).IsEqualTo(2);
        await Assert.That(sessions[1].Title).IsEqualTo("Original (Copy)");
    }
}
