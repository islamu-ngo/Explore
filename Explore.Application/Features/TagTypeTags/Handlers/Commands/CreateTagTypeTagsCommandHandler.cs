using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TagTypeTags.Validators;
using Explore.Application.Features.TagTypeTags.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.TagTypeTags.Handlers.Commands
{
    public class CreateTagTypeTagsCommandHandler : IRequestHandler<CreateTagTypeTagsCommand, BaseCommandResponse<Guid>>
    {
        private readonly ITagTypeTagsRepository _repository;
        private readonly IMapper _mapper;
        private readonly ITagRepository _tagRepository;
        private readonly ITagTypeRepository _tagTypeRepository;

        public CreateTagTypeTagsCommandHandler(
            ITagTypeTagsRepository repository,
            IMapper mapper,
            ITagRepository tagRepository,
            ITagTypeRepository tagTypeRepository)
        {
            _repository = repository;
            _mapper = mapper;
            _tagRepository = tagRepository;
            _tagTypeRepository = tagTypeRepository;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(CreateTagTypeTagsCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            var validator = new CreateTagTypeTagsDtoValidator(_tagRepository, _tagTypeRepository, _repository);
            var validationResult = await validator.ValidateAsync(request.TagTypeTagsDto, cancellationToken);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "Tag Type Tags creation failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            var tagTypeTags = _mapper.Map<Domain.TagTypeTags>(request.TagTypeTagsDto);
            tagTypeTags = await _repository.Create(tagTypeTags);

            response.Success = true;
            response.Id = tagTypeTags.Id;
            response.Message = "Tag Type Tags created successfully.";

            return response;
        }
    }
}
