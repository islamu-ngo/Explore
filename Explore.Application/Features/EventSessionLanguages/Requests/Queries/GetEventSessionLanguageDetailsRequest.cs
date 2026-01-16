using Explore.Application.DTOs.EventSessionLanguage;
using MediatR;

namespace Explore.Application.Features.EventSessionLanguages.Requests.Queries
{
    public class GetEventSessionLanguageDetailsRequest : IRequest<EventSessionLanguageDto>
    {
        public int Id { get; set; }
    }
}
