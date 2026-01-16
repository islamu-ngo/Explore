using MediatR;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.ActorKeyStore;
using Explore.Application.Features.ActorKeyStores.Requests.Queries;
using System.Collections.Generic;

namespace Explore.Application.Features.ActorKeyStores.Handlers.Queries
{
    public class GetActorKeyStoreListRequestHandler : IRequestHandler<GetActorKeyStoreListRequest, List<ActorKeyStoreListDto>>
    {
        private readonly IActorKeyStoreRepository _actorKeyStoreRepository;
        private readonly IMapper _mapper;

        public GetActorKeyStoreListRequestHandler(IActorKeyStoreRepository actorKeyStoreRepository, IMapper mapper)
        {
            _actorKeyStoreRepository = actorKeyStoreRepository;
            _mapper = mapper;
        }

        public async Task<List<ActorKeyStoreListDto>> Handle(GetActorKeyStoreListRequest request, CancellationToken cancellationToken)
        {
            var keyStores = await _actorKeyStoreRepository.GetActorKeyStoresWithDetails();
            return _mapper.Map<List<ActorKeyStoreListDto>>(keyStores);
        }
    }
}
