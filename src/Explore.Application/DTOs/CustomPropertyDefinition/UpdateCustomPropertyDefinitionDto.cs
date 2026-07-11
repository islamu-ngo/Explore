// ABOUTME: Write DTO for updating a shared Layer 3 custom-property definition and replacing its option set.
// ABOUTME: Mirrors create payload shape while carrying the existing definition identifier.

using Explore.Domain.Enums;

namespace Explore.Application.DTOs.CustomPropertyDefinition;

public class UpdateCustomPropertyDefinitionDto : CreateCustomPropertyDefinitionDto
{
    public Guid Id { get; set; }
    public Guid ExpectedConcurrencyStamp { get; set; }
}
