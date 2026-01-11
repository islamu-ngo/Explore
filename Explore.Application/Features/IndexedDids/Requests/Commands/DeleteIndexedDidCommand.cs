using MediatR;

namespace Explore.Application.Features.IndexedDids.Requests.Commands
{
    public class DeleteIndexedDidCommand : IRequest<bool>
    {
        public string Did { get; set; }
    }
}
