using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionLanguage.Validators;
using Explore.Application.Features.EventSessionLanguages.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.EventSessionLanguages.Handlers.Commands
{
    public class CreateEventSessionLanguageCommandHandler : IRequestHandler<CreateEventSessionLanguageCommand, BaseCommandResponse<int>>
    {
        private readonly IEventSessionLanguageRepository _sessionLanguageRepository;
        private readonly IEventSessionRepository _eventSessionRepository;
        private readonly ILanguageRepository _languageRepository;
        private readonly IMapper _mapper;

        public CreateEventSessionLanguageCommandHandler(
            IEventSessionLanguageRepository sessionLanguageRepository,
            IEventSessionRepository eventSessionRepository,
            ILanguageRepository languageRepository,
            IMapper mapper)
        {
            _sessionLanguageRepository = sessionLanguageRepository;
            _eventSessionRepository = eventSessionRepository;
            _languageRepository = languageRepository;
            _mapper = mapper;
        }

        public async Task<BaseCommandResponse<int>> Handle(CreateEventSessionLanguageCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<int>();

            var validator = new CreateEventSessionLanguageDtoValidator(_eventSessionRepository, _languageRepository);
            var validationResult = await validator.ValidateAsync(request.SessionLanguageDto);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "Session language assignment creation failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            var sessionLanguage = _mapper.Map<EventSessionLanguage>(request.SessionLanguageDto);

            sessionLanguage = await _sessionLanguageRepository.Create(sessionLanguage);

            response.Success = true;
            response.Id = sessionLanguage.Id;
            response.Message = "Language assigned to session successfully.";

            return response;
        }
    }
}
