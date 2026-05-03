// ABOUTME: Write DTO for editing event-local custom property definitions after instantiation.
// ABOUTME: Used when organizers customize template-derived definitions for their specific event (task 5.6).

namespace Explore.Application.DTOs.EventCustomProperty;

public class UpdateEventCustomPropertyDefinitionDto : CreateEventCustomPropertyDefinitionDto
{
    public Guid Id { get; set; }
    public Guid ExpectedConcurrencyStamp { get; set; }
}
