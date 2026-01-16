using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TagTypeTags.Validators;
using Explore.Application.Features.TagTypeTags.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.TagTypeTags.Handlers.Commands
{
    public class UpdateTagTypeTagsCommandHandler : IRequestHandler<UpdateTagTypeTagsCommand, BaseCommandResponse<Guid>>
    {
        private readonly ITagTypeTagsRepository _repository;
        private readonly IMapper _mapper;
        private readonly ITagRepository _tagRepository;
        private readonly ITagTypeRepository _tagTypeRepository;

        public UpdateTagTypeTagsCommandHandler(
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

        public async Task<BaseCommandResponse<Guid>> Handle(UpdateTagTypeTagsCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            var validator = new UpdateTagTypeTagsDtoValidator(_tagRepository, _tagTypeRepository);
            var validationResult = await validator.ValidateAsync(request.TagTypeTagsDto, cancellationToken);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "Tag Type Tags update failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            var tagTypeTags = await _repository.GetById(request.TagTypeTagsDto.Id);
            if (tagTypeTags == null)
            {
                response.Success = false;
                response.Message = "Tag Type Tags not found.";
                return response;
            }

            _mapper.Map(request.TagTypeTagsDto, tagTypeTags);
            await _repository.Update(tagTypeTags);

            response.Success = true;
            response.Id = tagTypeTags.Id;
            response.Message = "Tag Type Tags updated successfully.";

            return response;
        }
    }
}
