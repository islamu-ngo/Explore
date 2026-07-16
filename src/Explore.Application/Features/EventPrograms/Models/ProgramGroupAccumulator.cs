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
        string? color)
    {
        SectionKey = sectionKey;
        SessionGroupId = sessionGroupId;
        Title = title;
        SortOrder = sortOrder;
        Color = color;
    }

    public string SectionKey { get; }
    public Guid? SessionGroupId { get; }
    public string Title { get; }
    public int SortOrder { get; }
    public string? Color { get; }
    public List<EventProgramItemDto> Items { get; } = [];

    public static ProgramGroupAccumulator FromGroup(EventSessionGroup group)
    {
        return new ProgramGroupAccumulator(
            group.Id.ToString(),
            group.Id,
            group.Name,
            group.SortOrder,
            group.Color);
    }

    public static ProgramGroupAccumulator Unassigned(string sectionKey, int sortOrder)
    {
        return new ProgramGroupAccumulator(
            sectionKey,
            null,
            "Unassigned program items",
            sortOrder,
            null);
    }
}
