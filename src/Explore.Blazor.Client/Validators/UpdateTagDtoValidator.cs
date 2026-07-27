using Explore.Blazor.Client.Clients;
using FluentValidation;

namespace Explore.Blazor.Client.Validators;

public class UpdateTagDtoValidator : AbstractValidator<UpdateTagDto>
{
    public UpdateTagDtoValidator()
    {
        RuleFor(x => x.FullName).NotNull();
        RuleFor(x => x.FullName!.Value).NotEmpty().When(x => x.FullName is not null);
        RuleFor(x => x.MasterCode).NotNull();
        RuleFor(x => x.MasterCode!.Value).NotEmpty().When(x => x.MasterCode is not null);
    }
}
