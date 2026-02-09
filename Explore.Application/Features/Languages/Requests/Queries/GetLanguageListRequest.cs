using System.Collections.Generic;
using Explore.Application.DTOs.Language;
using MediatR;

namespace Explore.Application.Features.Languages.Requests.Queries;

public class GetLanguageListRequest : IRequest<List<LanguageListDto>>
{
}
