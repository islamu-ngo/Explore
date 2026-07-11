// ABOUTME: Write DTO for updating event session templates, extends CreateEventSessionTemplateDto with Id.
// ABOUTME: Replaces all definitions on update (full replacement, not partial patch).

namespace Explore.Application.DTOs.EventSessionTemplate;

public class UpdateEventSessionTemplateDto : CreateEventSessionTemplateDto
{
    public Guid Id { get; set; }
}
