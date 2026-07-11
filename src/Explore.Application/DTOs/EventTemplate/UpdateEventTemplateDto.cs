// ABOUTME: Write DTO for updating event templates, extends CreateEventTemplateDto with Id.
// ABOUTME: Replaces all definitions on update (full replacement, not partial patch).

namespace Explore.Application.DTOs.EventTemplate;

public class UpdateEventTemplateDto : CreateEventTemplateDto
{
    public Guid Id { get; set; }
}
