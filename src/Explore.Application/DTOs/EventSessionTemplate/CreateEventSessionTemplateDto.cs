// ABOUTME: Write DTO for creating event session templates, optionally includes nested definition payloads.
// ABOUTME: Session templates are owned children of event templates, not standalone reusable catalogs.

namespace Explore.Application.DTOs.EventSessionTemplate;

public sealed record CreateEventSessionTemplateDto
{
    private IReadOnlyList<CreateEventSessionTemplateDefinitionDto>? _definitions = Array.AsReadOnly(Array.Empty<CreateEventSessionTemplateDefinitionDto>());

    public Guid EventTemplateId { get; init; }
    public string SessionTemplateKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Version { get; set; } = 1;
    public bool IsPublished { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public IReadOnlyList<CreateEventSessionTemplateDefinitionDto> Definitions
    {
        get => _definitions!;
        init => _definitions = value is null ? null : Array.AsReadOnly(value.ToArray());
    }
}
