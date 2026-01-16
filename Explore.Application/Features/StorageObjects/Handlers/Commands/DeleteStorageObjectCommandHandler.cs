using MediatR;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.StorageObjects.Requests.Commands;
using Explore.Application.Responses;

namespace Explore.Application.Features.StorageObjects.Handlers.Commands
{
    public class DeleteStorageObjectCommandHandler : IRequestHandler<DeleteStorageObjectCommand, bool>
    {
        private readonly IStorageObjectRepository _storageObjectRepository;

        public DeleteStorageObjectCommandHandler(IStorageObjectRepository storageObjectRepository)
        {
            _storageObjectRepository = storageObjectRepository;
        }

        public async Task<bool> Handle(DeleteStorageObjectCommand request, CancellationToken cancellationToken)
        {
            var entity = await _storageObjectRepository.GetById(request.Id);

            if (entity == null)
            {
                return false;
            }

            await _storageObjectRepository.Delete(entity);

            return true;
        }
    }
}
