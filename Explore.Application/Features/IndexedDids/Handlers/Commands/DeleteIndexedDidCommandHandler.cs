using MediatR;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Application.Features.IndexedDids.Requests.Commands;

namespace Explore.Application.Features.IndexedDids.Handlers.Commands
{
    public class DeleteIndexedDidCommandHandler : IRequestHandler<DeleteIndexedDidCommand, bool>
    {
        private readonly IIndexedDidRepository _indexedDidRepository;

        public DeleteIndexedDidCommandHandler(IIndexedDidRepository indexedDidRepository)
        {
            _indexedDidRepository = indexedDidRepository;
        }

        public async Task<bool> Handle(DeleteIndexedDidCommand request, CancellationToken cancellationToken)
        {
            var indexedDid = await _indexedDidRepository.GetById(request.Did);
            if (indexedDid == null)
            {
                return false;
            }

            await _indexedDidRepository.Delete(indexedDid);
            return true;
        }
    }
}
