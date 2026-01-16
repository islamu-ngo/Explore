using MediatR;

namespace Explore.Application.Features.EventSessionLanguages.Requests.Commands
{
    public class DeleteEventSessionLanguageCommand : IRequest<bool>
    {
        public int Id { get; set; }
    }
}
