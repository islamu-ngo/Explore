// ABOUTME: Internal accumulator used while assembling server-backed event program summaries.
// ABOUTME: Lives outside handler namespaces so architecture rules only inspect real handler classes there.

using Explore.Application.DTOs.EventProgram;
using Explore.Domain;

namespace Explore.Application.Features.EventPrograms.Models;

internal sealed class ProgramGroupAccumulator
{
    private ProgramGroupAccumulator(
        string sectionKey,
        Guid? sessionGroupId,
        string title,
        int sortOrder,
        string? color,
        string? locationName,
        string? roomName)
    {
        SectionKey = sectionKey;
        SessionGroupId = sessionGroupId;
        Title = title;
        SortOrder = sortOrder;
        Color = color;
        LocationName = locationName;
        RoomName = roomName;
    }

    public string SectionKey { get; }
    public Guid? SessionGroupId { get; }
    public string Title { get; }
    public int SortOrder { get; }
    public string? Color { get; }
    public string? LocationName { get; }
    public string? RoomName { get; }
    public List<EventProgramItemDto> Items { get; } = [];

    public static ProgramGroupAccumulator FromGroup(EventSessionGroup group)
    {
        return new ProgramGroupAccumulator(
            group.Id.ToString(),
            group.Id,
            group.Name,
            group.SortOrder,
            group.Color,
            group.Location?.FullName,
            group.Room?.Name);
    }

    public static ProgramGroupAccumulator Unassigned(string sectionKey, int sortOrder)
    {
        return new ProgramGroupAccumulator(
            sectionKey,
            null,
            "Unassigned program items",
            sortOrder,
            null,
            null,
            null);
    }
}
