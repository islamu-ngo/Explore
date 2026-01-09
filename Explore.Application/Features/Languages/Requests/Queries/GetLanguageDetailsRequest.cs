using Explore.Application.DTOs.Language;
using MediatR;

namespace Explore.Application.Features.Languages.Requests.Queries
{
    public class GetLanguageDetailsRequest : IRequest<LanguageDto>
    {
        public int Id { get; set; }
    }
}
