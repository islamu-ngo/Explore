// ABOUTME: Write DTO for updating session-local custom property definitions, extends create with Id.
// ABOUTME: Provenance fields are read-only and preserved automatically by AutoMapper ignores.

namespace Explore.Application.DTOs.EventSessionCustomProperty;

public class UpdateEventSessionCustomPropertyDefinitionDto : CreateEventSessionCustomPropertyDefinitionDto
{
    public Guid Id { get; set; }
}
