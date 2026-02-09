using Explore.Application.DTOs.EventSessionLanguage;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionLanguages.Requests.Commands;

public class UpdateEventSessionLanguageCommand : IRequest<BaseCommandResponse<int>>
{
    public required UpdateEventSessionLanguageDto EventSessionLanguageDto { get; set; }
}
