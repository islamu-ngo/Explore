using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventCategories.Validators;
using Explore.Application.Features.EventCategories.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.EventCategories.Handlers.Commands
{
    public class CreateEventCategoriesCommandHandler : IRequestHandler<CreateEventCategoriesCommand, BaseCommandResponse<Guid>>
    {
        private readonly IEventCategoriesRepository _eventCategoriesRepository;
        private readonly IEventRepository _eventRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;

        public CreateEventCategoriesCommandHandler(
            IEventCategoriesRepository eventCategoriesRepository,
            IEventRepository eventRepository,
            ICategoryRepository categoryRepository,
            IMapper mapper)
        {
            _eventCategoriesRepository = eventCategoriesRepository;
            _eventRepository = eventRepository;
            _categoryRepository = categoryRepository;
            _mapper = mapper;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCategoriesCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            var validator = new CreateEventCategoriesDtoValidator(_eventRepository, _categoryRepository, _eventCategoriesRepository);
            var validationResult = await validator.ValidateAsync(request.EventCategoriesDto);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "Event Category assignment failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            var eventCategories = _mapper.Map<EventCategories>(request.EventCategoriesDto);
            eventCategories = await _eventCategoriesRepository.Create(eventCategories);

            response.Success = true;
            response.Id = eventCategories.Id;
            response.Message = "Event Category assigned successfully.";

            return response;
        }
    }
}
