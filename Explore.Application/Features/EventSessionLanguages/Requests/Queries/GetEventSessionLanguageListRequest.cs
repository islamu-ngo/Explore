using System.Collections.Generic;
using Explore.Application.DTOs.EventSessionLanguage;
using MediatR;

namespace Explore.Application.Features.EventSessionLanguages.Requests.Queries
{
    public class GetEventSessionLanguageListRequest : IRequest<List<EventSessionLanguageListDto>>
    {
    }
}
