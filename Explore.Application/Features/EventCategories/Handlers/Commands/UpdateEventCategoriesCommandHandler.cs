using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventCategories.Validators;
using Explore.Application.Features.EventCategories.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventCategories.Handlers.Commands;

public class UpdateEventCategoriesCommandHandler : IRequestHandler<UpdateEventCategoriesCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventCategoriesRepository _eventCategoriesRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IMapper _mapper;

    public UpdateEventCategoriesCommandHandler(
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

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventCategoriesCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateEventCategoriesDtoValidator(_eventRepository, _categoryRepository, _eventCategoriesRepository);
        var validationResult = await validator.ValidateAsync(request.EventCategoriesDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event Category update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var eventCategories = await _eventCategoriesRepository.GetById(request.EventCategoriesDto.Id);

        if (eventCategories == null)
        {
            response.Success = false;
            response.Message = "Event Category not found.";
            return response;
        }

        _mapper.Map(request.EventCategoriesDto, eventCategories);
        await _eventCategoriesRepository.Update(eventCategories);

        response.Success = true;
        response.Id = eventCategories.Id;
        response.Message = "Event Category updated successfully.";

        return response;
    }
}
