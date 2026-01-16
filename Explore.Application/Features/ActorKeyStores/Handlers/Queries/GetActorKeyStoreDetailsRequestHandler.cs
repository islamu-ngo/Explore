using MediatR;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.ActorKeyStore;
using Explore.Application.Features.ActorKeyStores.Requests.Queries;

namespace Explore.Application.Features.ActorKeyStores.Handlers.Queries
{
    public class GetActorKeyStoreDetailsRequestHandler : IRequestHandler<GetActorKeyStoreDetailsRequest, ActorKeyStoreDto>
    {
        private readonly IActorKeyStoreRepository _actorKeyStoreRepository;
        private readonly IMapper _mapper;

        public GetActorKeyStoreDetailsRequestHandler(IActorKeyStoreRepository actorKeyStoreRepository, IMapper mapper)
        {
            _actorKeyStoreRepository = actorKeyStoreRepository;
            _mapper = mapper;
        }

        public async Task<ActorKeyStoreDto> Handle(GetActorKeyStoreDetailsRequest request, CancellationToken cancellationToken)
        {
            var keyStore = await _actorKeyStoreRepository.GetActorKeyStoreWithDetails(request.Id);
            if (keyStore == null)
            {
                return null;
            }

            return _mapper.Map<ActorKeyStoreDto>(keyStore);
        }
    }
}
