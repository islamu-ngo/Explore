using Explore.Application.DTOs.EventSessionLanguage;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionLanguages.Requests.Commands
{
    public class CreateEventSessionLanguageCommand : IRequest<BaseCommandResponse<int>>
    {
        public CreateEventSessionLanguageDto EventSessionLanguageDto { get; set; }
    }
}
