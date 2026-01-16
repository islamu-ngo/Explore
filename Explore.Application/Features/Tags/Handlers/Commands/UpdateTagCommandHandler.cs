using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Tag.Validators;
using Explore.Application.Features.Tags.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Tags.Handlers.Commands
{
    public class UpdateTagCommandHandler : IRequestHandler<UpdateTagCommand, BaseCommandResponse<Guid>>
    {
        private readonly ITagRepository _tagRepository;
        private readonly IMapper _mapper;

        public UpdateTagCommandHandler(
            ITagRepository tagRepository,
            IMapper mapper)
        {
            _tagRepository = tagRepository;
            _mapper = mapper;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(UpdateTagCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            var validator = new UpdateTagDtoValidator();
            var validationResult = await validator.ValidateAsync(request.TagDto);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "Tag update failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            var tag = await _tagRepository.GetById(request.TagDto.Id);

            if (tag == null)
            {
                response.Success = false;
                response.Message = "Tag not found.";
                return response;
            }

            _mapper.Map(request.TagDto, tag);

            await _tagRepository.Update(tag);

            response.Success = true;
            response.Id = tag.Id;
            response.Message = "Tag updated successfully.";

            return response;
        }
    }
}
