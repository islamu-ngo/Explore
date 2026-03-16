// ABOUTME: Shared workflow state and transitions for the Create/Edit event session drawer.
// ABOUTME: Centralizes drawer open/close, navigation, save, duplication, and default session creation.

using Explore.Blazor.Client.Pages.Events.Models;

namespace Explore.Blazor.Client.Pages.Events.Workflows;

public sealed class SessionEditorWorkflow
{
    public bool IsDrawerOpen { get; private set; }

    public bool IsNewSession { get; private set; }

    public int EditingSessionIndex { get; private set; } = -1;

    public SessionEditorModel? DrawerModel { get; private set; }

    public bool CanNavigatePrevious(IList<SessionEditorModel> sessions)
    {
        return !IsNewSession && EditingSessionIndex > 0 && sessions.Count > 0;
    }

    public bool CanNavigateNext(IList<SessionEditorModel> sessions)
    {
        return !IsNewSession && EditingSessionIndex >= 0 && EditingSessionIndex < sessions.Count - 1;
    }

    public SessionEditorModel CreateDefaultSession(IList<SessionEditorModel> sessions, string? eventImageUrl)
    {
        var defaultStart = DateTime.Today.AddDays(1).AddHours(9);
        var defaultEnd = DateTime.Today.AddDays(1).AddHours(17);

        if (sessions.Count > 0)
        {
            var last = sessions[^1];
            defaultStart = last.StartTime.AddDays(1);
            defaultEnd = last.EndTime.AddDays(1);
        }

        return new SessionEditorModel
        {
            StartTime = defaultStart,
            EndTime = defaultEnd,
            RegistrationModeId = sessions.FirstOrDefault()?.RegistrationModeId ?? 1,
            LocationId = sessions.FirstOrDefault()?.LocationId,
            UseEventImage = true,
            FeaturedImagePreviewUrl = eventImageUrl
        };
    }

    public void OpenForEdit(IList<SessionEditorModel> sessions, int editIndex)
    {
        if (editIndex < 0 || editIndex >= sessions.Count)
        {
            return;
        }

        IsNewSession = false;
        EditingSessionIndex = editIndex;
        DrawerModel = Copy(sessions[editIndex]);
        IsDrawerOpen = true;
    }

    public void OpenForCreate(IList<SessionEditorModel> sessions, string? eventImageUrl)
    {
        IsNewSession = true;
        EditingSessionIndex = -1;
        DrawerModel = CreateDefaultSession(sessions, eventImageUrl);
        IsDrawerOpen = true;
    }

    public void Close()
    {
        IsDrawerOpen = false;
        DrawerModel = null;
        EditingSessionIndex = -1;
    }

    public void SetOpen(bool open)
    {
        IsDrawerOpen = open;
        if (!open)
        {
            DrawerModel = null;
            EditingSessionIndex = -1;
        }
    }

    public void SaveSession(IList<SessionEditorModel> sessions, SessionEditorModel model)
    {
        if (IsNewSession)
        {
            sessions.Add(model);
        }
        else if (EditingSessionIndex >= 0 && EditingSessionIndex < sessions.Count)
        {
            sessions[EditingSessionIndex] = model;
        }

        Close();
    }

    public bool DuplicateSession(IList<SessionEditorModel> sessions, int index)
    {
        if (index < 0 || index >= sessions.Count)
        {
            return false;
        }

        sessions.Add(sessions[index].Clone());
        return true;
    }

    public void NavigatePrevious(IList<SessionEditorModel> sessions, string? eventImageUrl)
    {
        if (!CanNavigatePrevious(sessions))
        {
            return;
        }

        SaveCurrentDrawerSession(sessions);
        OpenForEdit(sessions, EditingSessionIndex - 1);
    }

    public void NavigateNext(IList<SessionEditorModel> sessions, string? eventImageUrl)
    {
        if (!CanNavigateNext(sessions))
        {
            return;
        }

        SaveCurrentDrawerSession(sessions);
        OpenForEdit(sessions, EditingSessionIndex + 1);
    }

    public void AddFromDrawer(IList<SessionEditorModel> sessions, string? eventImageUrl)
    {
        SaveCurrentDrawerSession(sessions);
        OpenForCreate(sessions, eventImageUrl);
    }

    public void SaveCurrentDrawerSession(IList<SessionEditorModel> sessions)
    {
        if (DrawerModel is null)
        {
            return;
        }

        if (IsNewSession)
        {
            sessions.Add(DrawerModel);
        }
        else if (EditingSessionIndex >= 0 && EditingSessionIndex < sessions.Count)
        {
            sessions[EditingSessionIndex] = DrawerModel;
        }
    }

    private static SessionEditorModel Copy(SessionEditorModel source)
    {
        return new SessionEditorModel
        {
            Id = source.Id,
            Title = source.Title,
            Description = source.Description,
            StartTime = source.StartTime,
            EndTime = source.EndTime,
            LocationId = source.LocationId,
            MaxAudienceAttendees = source.MaxAudienceAttendees,
            RegistrationModeId = source.RegistrationModeId,
            LanguageIds = new HashSet<int>(source.LanguageIds),
            FeaturedImageId = source.FeaturedImageId,
            FeaturedImagePreviewUrl = source.FeaturedImagePreviewUrl,
            UseEventImage = source.UseEventImage,
            PendingImageBytes = source.PendingImageBytes,
            PendingImageFileName = source.PendingImageFileName
        };
    }
}
