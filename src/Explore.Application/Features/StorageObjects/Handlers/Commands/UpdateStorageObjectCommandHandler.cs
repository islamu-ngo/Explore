// ABOUTME: Handler for updating storage object metadata with validation.
// ABOUTME: Validates input, fetches entity, applies field updates.
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.StorageObject;
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
    private readonly ITenantContext _tenantContext;

    public UpdateStorageObjectCommandHandler(
        IStorageObjectRepository storageObjectRepository,
        IFileTypeRepository fileTypeRepository,
        IActorRepository actorRepository,
        ITenantContext tenantContext)
    {
        _storageObjectRepository = storageObjectRepository;
        _fileTypeRepository = fileTypeRepository;
        _actorRepository = actorRepository;
        _tenantContext = tenantContext;
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

        var entity = await _storageObjectRepository.GetById(request.StorageObjectDto.Id);
        if (entity is null || entity.TenantId != _tenantContext.TenantId)
        {
            response.Success = false;
            response.Message = "Storage object update failed.";
            response.Errors = ["Storage object not found."];
            return response;
        }

        ApplyUpdate(entity, request.StorageObjectDto);
        await _storageObjectRepository.Update(entity);

        response.Success = true;
        response.Id = entity.Id;
        response.Message = "Storage object updated successfully.";

        return response;
    }

    private static void ApplyUpdate(Domain.StorageObject entity, UpdateStorageObjectDto dto)
    {
        entity.FileTypeId = dto.FileTypeId;
        entity.Uri = dto.Uri;
        entity.ObjectKey = dto.ObjectKey;
        entity.Provider = dto.Provider;
        entity.FullName = dto.FullName;
        entity.SafeDisplayName = string.IsNullOrWhiteSpace(dto.SafeDisplayName) ? dto.FullName : dto.SafeDisplayName;
        entity.Extension = dto.Extension;
        entity.ContentType = dto.ContentType;
        entity.Sha256Checksum = dto.Sha256Checksum;
        entity.Size = dto.Size;
        entity.Visibility = dto.Visibility;
        entity.Purpose = dto.Purpose;
        entity.LifecycleState = dto.LifecycleState;
        entity.OwningResourceKind = dto.OwningResourceKind;
        entity.OwningResourceId = dto.OwningResourceId;
        entity.ActorId = dto.ActorId;
    }
}
