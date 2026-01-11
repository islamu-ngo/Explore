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
    public class CreateEventTagsCommandHandler : IRequestHandler<CreateEventTagsCommand, BaseCommandResponse<Guid>>
    {
        private readonly IEventTagsRepository _eventTagsRepository;
        private readonly IEventRepository _eventRepository;
        private readonly ITagRepository _tagRepository;
        private readonly IMapper _mapper;

        public CreateEventTagsCommandHandler(
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

        public async Task<BaseCommandResponse<Guid>> Handle(CreateEventTagsCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            var validator = new CreateEventTagsDtoValidator(_eventRepository, _tagRepository, _eventTagsRepository);
            var validationResult = await validator.ValidateAsync(request.EventTagsDto);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "Event Tag assignment failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            var eventTags = _mapper.Map<Domain.EventTags>(request.EventTagsDto);
            eventTags = await _eventTagsRepository.Create(eventTags);

            response.Success = true;
            response.Id = eventTags.Id;
            response.Message = "Event Tag assigned successfully.";

            return response;
        }
    }
}
