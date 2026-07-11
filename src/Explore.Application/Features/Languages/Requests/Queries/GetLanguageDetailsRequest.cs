// ABOUTME: MediatR query request for fetching a single language by ID.
// ABOUTME: Returns LanguageDto.
using Explore.Application.DTOs.Language;
using MediatR;

namespace Explore.Application.Features.Languages.Requests.Queries;

public class GetLanguageDetailsRequest : IRequest<LanguageDto>
{
    public int Id { get; set; }
}
