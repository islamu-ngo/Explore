using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.TagTypeTags.Validators;

public class UpdateTagTypeTagsDtoValidator : AbstractValidator<UpdateTagTypeTagsDto>
{
    private readonly ITagRepository _tagRepository;
    private readonly ITagTypeRepository _tagTypeRepository;

    public UpdateTagTypeTagsDtoValidator(
        ITagRepository tagRepository,
        ITagTypeRepository tagTypeRepository)
    {
        _tagRepository = tagRepository;
        _tagTypeRepository = tagTypeRepository;

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required");

        RuleFor(x => x.TagId)
            .NotEmpty().WithMessage("Tag is required")
            .MustAsync(TagExists)
            .WithMessage("Tag not found");

        RuleFor(x => x.TagTypeId)
            .NotEmpty().WithMessage("Tag Type is required")
            .MustAsync(TagTypeExists)
            .WithMessage("Tag Type not found");
    }

    private async Task<bool> TagExists(Guid tagId, CancellationToken cancellationToken)
    {
        return await _tagRepository.Exists(tagId);
    }

    private async Task<bool> TagTypeExists(int tagTypeId, CancellationToken cancellationToken)
    {
        return await _tagTypeRepository.Exists(tagTypeId);
    }
}
