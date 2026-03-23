// ABOUTME: Handler for updating a session-language link with validation.
// ABOUTME: Validates input, fetches entity, applies field updates.
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionLanguage.Validators;
using Explore.Application.Features.EventSessionLanguages.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionLanguages.Handlers.Commands;

public class UpdateEventSessionLanguageCommandHandler : IRequestHandler<UpdateEventSessionLanguageCommand, BaseCommandResponse<int>>
{
    private readonly IEventSessionLanguageRepository _repository;
    private readonly IMapper _mapper;
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly ILanguageRepository _languageRepository;

    public UpdateEventSessionLanguageCommandHandler(
        IEventSessionLanguageRepository repository,
        IMapper mapper,
        IEventSessionRepository eventSessionRepository,
        ILanguageRepository languageRepository)
    {
        _repository = repository;
        _mapper = mapper;
        _eventSessionRepository = eventSessionRepository;
        _languageRepository = languageRepository;
    }

    public async Task<BaseCommandResponse<int>> Handle(UpdateEventSessionLanguageCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<int>();

        var validator = new UpdateEventSessionLanguageDtoValidator(_eventSessionRepository, _languageRepository);
        var validationResult = await validator.ValidateAsync(request.EventSessionLanguageDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event Session Language update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var eventSessionLanguage = await _repository.GetById(request.EventSessionLanguageDto.Id);
        if (eventSessionLanguage == null)
        {
            response.Success = false;
            response.Message = "Event Session Language not found.";
            return response;
        }

        _mapper.Map(request.EventSessionLanguageDto, eventSessionLanguage);
        await _repository.Update(eventSessionLanguage);

        response.Success = true;
        response.Id = eventSessionLanguage.Id;
        response.Message = "Event Session Language updated successfully.";

        return response;
    }
}
