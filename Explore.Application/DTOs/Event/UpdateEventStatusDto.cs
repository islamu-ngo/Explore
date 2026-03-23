// ABOUTME: DTO for updating only the EventStatusId of an event (e.g., Draft→Published, Published→Cancelled).
// ABOUTME: Used with the UpdateEventCommand null-check pattern — ID comes from the URL.

namespace Explore.Application.DTOs.Event;

public class UpdateEventStatusDto
{
    public int EventStatusId { get; set; }
}
