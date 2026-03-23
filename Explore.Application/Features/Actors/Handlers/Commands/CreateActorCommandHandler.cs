// ABOUTME: Handler for creating a new actor entity with validation.
// ABOUTME: Validates input, maps DTO, sets TenantId, persists via repository.
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Actor.Validators;
using Explore.Application.Features.Actors.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.Actors.Handlers.Commands;

public class CreateActorCommandHandler : IRequestHandler<CreateActorCommand, BaseCommandResponse<Guid>>
{
    private readonly IActorRepository _actorRepository;
    private readonly IActorTypeRepository _actorTypeRepository;
    private readonly IDidCustodyTypeRepository _didCustodyTypeRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;

    public CreateActorCommandHandler(
        IActorRepository actorRepository,
        IActorTypeRepository actorTypeRepository,
        IDidCustodyTypeRepository didCustodyTypeRepository,
        IStorageObjectRepository storageObjectRepository,
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IOrganizationRepository organizationRepository,
        ITenantContext tenantContext,
        IMapper mapper)
    {
        _actorRepository = actorRepository;
        _actorTypeRepository = actorTypeRepository;
        _didCustodyTypeRepository = didCustodyTypeRepository;
        _storageObjectRepository = storageObjectRepository;
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _organizationRepository = organizationRepository;
        _tenantContext = tenantContext;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateActorCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new CreateActorDtoValidator(
            _actorTypeRepository,
            _didCustodyTypeRepository,
            _storageObjectRepository,
            _tenantRepository,
            _actorRepository,
            _userRepository,
            _organizationRepository);

        var validationResult = await validator.ValidateAsync(request.ActorDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Actor creation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var actor = _mapper.Map<Actor>(request.ActorDto);

        // Override TenantId with the value from the request context
        actor.TenantId = _tenantContext.TenantId;

        actor = await _actorRepository.Create(actor);

        response.Success = true;
        response.Id = actor.Id;
        response.Message = "Actor created successfully.";

        return response;
    }
}
