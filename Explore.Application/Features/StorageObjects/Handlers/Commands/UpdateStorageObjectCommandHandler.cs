// ABOUTME: Handler for updating storage object metadata with validation.
// ABOUTME: Validates input, fetches entity, applies field updates.
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.StorageObject.Validators;
using Explore.Application.Features.StorageObjects.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Handlers.Commands;

public class UpdateStorageObjectCommandHandler : IRequestHandler<UpdateStorageObjectCommand, BaseCommandResponse<Guid>>
{
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IFileTypeRepository _fileTypeRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IMapper _mapper;

    public UpdateStorageObjectCommandHandler(
        IStorageObjectRepository storageObjectRepository,
        IFileTypeRepository fileTypeRepository,
        IActorRepository actorRepository,
        IMapper mapper)
    {
        _storageObjectRepository = storageObjectRepository;
        _fileTypeRepository = fileTypeRepository;
        _actorRepository = actorRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateStorageObjectCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateStorageObjectDtoValidator(_fileTypeRepository, _actorRepository);
        var validationResult = await validator.ValidateAsync(request.StorageObjectDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Storage object update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        // Map DTO to Entity
        var entity = _mapper.Map<Domain.StorageObject>(request.StorageObjectDto);

        // Update through repository
        await _storageObjectRepository.Update(entity);

        response.Success = true;
        response.Id = entity.Id;
        response.Message = "Storage object updated successfully.";

        return response;
    }
}
