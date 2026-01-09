using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventSessionLanguages.Requests.Commands;
using MediatR;

namespace Explore.Application.Features.EventSessionLanguages.Handlers.Commands
{
    public class DeleteEventSessionLanguageCommandHandler : IRequestHandler<DeleteEventSessionLanguageCommand, bool>
    {
        private readonly IEventSessionLanguageRepository _sessionLanguageRepository;

        public DeleteEventSessionLanguageCommandHandler(IEventSessionLanguageRepository sessionLanguageRepository)
        {
            _sessionLanguageRepository = sessionLanguageRepository;
        }

        public async Task<bool> Handle(DeleteEventSessionLanguageCommand request, CancellationToken cancellationToken)
        {
            var sessionLanguage = await _sessionLanguageRepository.GetById(request.Id);

            if (sessionLanguage == null)
            {
                return false;
            }

            await _sessionLanguageRepository.Delete(sessionLanguage);

            return true;
        }
    }
}
