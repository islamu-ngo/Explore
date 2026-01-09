using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventTags.Validators;
using Explore.Application.Features.EventTags.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventTags.Handlers.Commands
{
    public class UpdateEventTagsCommandHandler : IRequestHandler<UpdateEventTagsCommand, BaseCommandResponse<Guid>>
    {
        private readonly IEventTagsRepository _eventTagsRepository;
        private readonly IEventRepository _eventRepository;
        private readonly ITagRepository _tagRepository;
        private readonly IMapper _mapper;

        public UpdateEventTagsCommandHandler(
            IEventTagsRepository eventTagsRepository,
            IEventRepository eventRepository,
            ITagRepository tagRepository,
            IMapper mapper)
        {
            _eventTagsRepository = eventTagsRepository;
            _eventRepository = eventRepository;
            _tagRepository = tagRepository;
            _mapper = mapper;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventTagsCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            var validator = new UpdateEventTagsDtoValidator(_eventRepository, _tagRepository, _eventTagsRepository);
            var validationResult = await validator.ValidateAsync(request.EventTagsDto);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "Event Tag update failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            var eventTags = await _eventTagsRepository.GetById(request.EventTagsDto.Id);

            if (eventTags == null)
            {
                response.Success = false;
                response.Message = "Event Tag not found.";
                return response;
            }

            _mapper.Map(request.EventTagsDto, eventTags);
            await _eventTagsRepository.Update(eventTags);

            response.Success = true;
            response.Id = eventTags.Id;
            response.Message = "Event Tag updated successfully.";

            return response;
        }
    }
}
