using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.TagTypeTags.Validators
{
    public class CreateTagTypeTagsDtoValidator : AbstractValidator<CreateTagTypeTagsDto>
    {
        private readonly ITagRepository _tagRepository;
        private readonly ITagTypeRepository _tagTypeRepository;
        private readonly ITagTypeTagsRepository _tagTypeTagsRepository;

        public CreateTagTypeTagsDtoValidator(
            ITagRepository tagRepository,
            ITagTypeRepository tagTypeRepository,
            ITagTypeTagsRepository tagTypeTagsRepository)
        {
            _tagRepository = tagRepository;
            _tagTypeRepository = tagTypeRepository;
            _tagTypeTagsRepository = tagTypeTagsRepository;

            RuleFor(x => x.TagId)
                .NotEmpty().WithMessage("Tag is required")
                .MustAsync(TagExists)
                .WithMessage("Tag not found");

            RuleFor(x => x.TagTypeId)
                .NotEmpty().WithMessage("Tag Type is required")
                .MustAsync(TagTypeExists)
                .WithMessage("Tag Type not found");

            RuleFor(x => x)
                .MustAsync(TagTypeTagNotExist)
                .WithMessage("This Tag is already assigned to this Tag Type");

            // TenantId is set by the handler from context, not by the client
            // No validation needed here
        }

        private async Task<bool> TagExists(Guid tagId, CancellationToken cancellationToken)
        {
            return await _tagRepository.Exists(tagId);
        }

        private async Task<bool> TagTypeExists(int tagTypeId, CancellationToken cancellationToken)
        {
            return await _tagTypeRepository.Exists(tagTypeId);
        }

        private async Task<bool> TagTypeTagNotExist(CreateTagTypeTagsDto dto, CancellationToken cancellationToken)
        {
            return !await _tagTypeTagsRepository.Exists(dto.TagId, dto.TagTypeId);
        }
    }
}
