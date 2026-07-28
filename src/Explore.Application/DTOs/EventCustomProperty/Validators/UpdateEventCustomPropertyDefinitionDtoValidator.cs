// ABOUTME: Validates event-local custom-property definition update payload shape.
// ABOUTME: Reuses create validation; route ID and If-Match are handled at the API boundary.

using FluentValidation;

namespace Explore.Application.DTOs.EventCustomProperty.Validators;

public class UpdateEventCustomPropertyDefinitionDtoValidator : AbstractValidator<UpdateEventCustomPropertyDefinitionDto>
{
    public UpdateEventCustomPropertyDefinitionDtoValidator()
    {
        Include(new CreateEventCustomPropertyDefinitionDtoValidator());
    }
}
