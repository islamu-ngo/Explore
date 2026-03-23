// ABOUTME: Handler for creating a new storage object metadata record with validation.
// ABOUTME: Validates input, maps DTO, links to actor, persists via repository.
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.DTOs.StorageObject.Validators;
using Explore.Application.Features.StorageObjects.Requests.Commands;
using Explore.Application.Responses;
using FluentValidation;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Handlers.Commands;

public class CreateStorageObjectCommandHandler : IRequestHandler<CreateStorageObjectCommand, BaseCommandResponse<Guid>>
{
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IFileTypeRepository _fileTypeRepository;
    private readonly IActorRepository _actorRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;

    public CreateStorageObjectCommandHandler(
        IStorageObjectRepository storageObjectRepository,
        IFileTypeRepository fileTypeRepository,
        IActorRepository actorRepository,
        ITenantContext tenantContext,
        IMapper mapper)
    {
        _storageObjectRepository = storageObjectRepository;
        _fileTypeRepository = fileTypeRepository;
        _actorRepository = actorRepository;
        _tenantContext = tenantContext;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateStorageObjectCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        // Validate using FluentValidation
        var validator = new CreateStorageObjectDtoValidator(_fileTypeRepository, _actorRepository);
        var validationResult = await validator.ValidateAsync(request.StorageObjectDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Storage object creation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        // Map DTO to Entity
        var entity = _mapper.Map<Domain.StorageObject>(request.StorageObjectDto);

        // Set TenantId from the request context
        entity.TenantId = _tenantContext.TenantId;

        // Save through repository
        entity = await _storageObjectRepository.Create(entity);

        response.Success = true;
        response.Id = entity.Id;
        response.Message = "Storage object created successfully.";

        return response;
    }
}
