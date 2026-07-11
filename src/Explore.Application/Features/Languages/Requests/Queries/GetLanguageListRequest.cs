// ABOUTME: MediatR query request for fetching all available languages.
// ABOUTME: Returns IEnumerable<LanguageDto>.
using System.Collections.Generic;
using Explore.Application.DTOs.Language;
using MediatR;

namespace Explore.Application.Features.Languages.Requests.Queries;

public class GetLanguageListRequest : IRequest<List<LanguageListDto>>
{
}
