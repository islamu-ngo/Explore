using Explore.Blazor.Client.Clients;
using FluentValidation;

namespace Explore.Blazor.Client.Validators
{
    public class CreateLocationDtoValidator : AbstractValidator<CreateLocationDto>
    {
        public CreateLocationDtoValidator()
        {
            RuleFor(x => x.FullName).NotEmpty();
        }
    }
}
