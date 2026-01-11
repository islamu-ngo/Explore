using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Features.StorageObjects.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Handlers.Queries
{
    public class GetStorageObjectListRequestHandler : IRequestHandler<GetStorageObjectListRequest, List<StorageObjectListDto>>
    {
        private readonly IStorageObjectRepository _storageObjectRepository;
        private readonly IMapper _mapper;

        public GetStorageObjectListRequestHandler(IStorageObjectRepository storageObjectRepository, IMapper mapper)
        {
            _storageObjectRepository = storageObjectRepository;
            _mapper = mapper;
        }

        public async Task<List<StorageObjectListDto>> Handle(GetStorageObjectListRequest request, CancellationToken cancellationToken)
        {
            var storageObjects = await _storageObjectRepository.GetAll();
            return _mapper.Map<List<StorageObjectListDto>>(storageObjects);
        }
    }
}
