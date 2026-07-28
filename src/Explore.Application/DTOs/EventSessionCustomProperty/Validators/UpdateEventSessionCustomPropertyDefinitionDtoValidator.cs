// ABOUTME: Validates session-local custom-property definition update payload shape.
// ABOUTME: Reuses create validation; route ID and If-Match are handled at the API boundary.

using FluentValidation;

namespace Explore.Application.DTOs.EventSessionCustomProperty.Validators;

public class UpdateEventSessionCustomPropertyDefinitionDtoValidator : AbstractValidator<UpdateEventSessionCustomPropertyDefinitionDto>
{
    public UpdateEventSessionCustomPropertyDefinitionDtoValidator()
    {
        Include(new CreateEventSessionCustomPropertyDefinitionDtoValidator());
    }
}
