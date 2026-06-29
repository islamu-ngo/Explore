// ABOUTME: PATCH wrapper DTO for event-level agenda item updates using nullable logical groups.
// ABOUTME: Route ID targets the row; groups express independent property update intent.

namespace Explore.Application.DTOs.EventAgendaItem;

using Explore.Application.Models.Common;

public class UpdateEventAgendaItemDto
{
    public UpdateEventAgendaItemEventDto? Event { get; set; }
    public UpdateEventAgendaItemTitleDto? Title { get; set; }
    public UpdateEventAgendaItemDescriptionDto? Description { get; set; }
    public UpdateEventAgendaItemScheduleDto? Schedule { get; set; }
    public UpdateEventAgendaItemLocationDto? Location { get; set; }
    public UpdateEventAgendaItemRoomDto? Room { get; set; }
    public UpdateEventAgendaItemKindDto? Kind { get; set; }
    public UpdateEventAgendaItemSortOrderDto? SortOrder { get; set; }
}

public class UpdateEventAgendaItemEventDto
{
    public Guid EventId { get; set; }
}

public class UpdateEventAgendaItemTitleDto
{
    public required string Value { get; set; }
}

public class UpdateEventAgendaItemDescriptionDto
{
    public OptionalUpdate<string?> Value { get; set; } = OptionalUpdate<string?>.Unspecified();
}

public class UpdateEventAgendaItemScheduleDto
{
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
}

public class UpdateEventAgendaItemLocationDto
{
    public OptionalUpdate<Guid?> Value { get; set; } = OptionalUpdate<Guid?>.Unspecified();
}

public class UpdateEventAgendaItemRoomDto
{
    public OptionalUpdate<Guid?> Value { get; set; } = OptionalUpdate<Guid?>.Unspecified();
}

public class UpdateEventAgendaItemKindDto
{
    public OptionalUpdate<int?> Value { get; set; } = OptionalUpdate<int?>.Unspecified();
}

public class UpdateEventAgendaItemSortOrderDto
{
    public int Value { get; set; }
}
