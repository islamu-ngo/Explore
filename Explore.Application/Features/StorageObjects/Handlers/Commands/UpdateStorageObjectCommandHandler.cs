using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.DTOs.StorageObject.Validators;
using Explore.Application.Features.StorageObjects.Requests.Commands;
using Explore.Application.Responses;
using FluentValidation;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Handlers.Commands;

public class UpdateStorageObjectCommandHandler : IRequestHandler<UpdateStorageObjectCommand, BaseCommandResponse<Guid>>
{
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<UpdateStorageObjectDto> _validator;

    public UpdateStorageObjectCommandHandler(
        IStorageObjectRepository storageObjectRepository,
        IMapper mapper,
        IValidator<UpdateStorageObjectDto> validator)
    {
        _storageObjectRepository = storageObjectRepository;
        _mapper = mapper;
        _validator = validator;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateStorageObjectCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        // Validate using FluentValidation
        var validationResult = await _validator.ValidateAsync(request.StorageObjectDto, cancellationToken);

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
