using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.ActorKeyStore.Validators;
using Explore.Application.Features.ActorKeyStores.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using FluentValidation;
using MediatR;

namespace Explore.Application.Features.ActorKeyStores.Handlers.Commands;

public class CreateActorKeyStoreCommandHandler : IRequestHandler<CreateActorKeyStoreCommand, BaseCommandResponse<Guid>>
{
    private readonly IActorKeyStoreRepository _actorKeyStoreRepository;
    private readonly IActorRepository _actorRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;

    public CreateActorKeyStoreCommandHandler(
        IActorKeyStoreRepository actorKeyStoreRepository,
        IActorRepository actorRepository,
        ITenantRepository tenantRepository,
        ITenantContext tenantContext,
        IMapper mapper)
    {
        _actorKeyStoreRepository = actorKeyStoreRepository;
        _actorRepository = actorRepository;
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateActorKeyStoreCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new CreateActorKeyStoreDtoValidator(_actorRepository, _tenantRepository);
        var validationResult = await validator.ValidateAsync(request.ActorKeyStoreDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Actor Key Store creation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var keyStore = _mapper.Map<ActorKeyStore>(request.ActorKeyStoreDto);
        keyStore.CreatedAt = DateTime.UtcNow;

        // Set TenantId from request context
        keyStore.TenantId = _tenantContext.TenantId;

        keyStore = await _actorKeyStoreRepository.Create(keyStore);

        response.Success = true;
        response.Id = keyStore.Id;
        response.Message = "Actor Key Store created successfully.";

        return response;
    }
}
