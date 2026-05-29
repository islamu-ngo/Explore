// ABOUTME: Handler for generating a pre-signed upload URL for direct client-side storage upload.
// ABOUTME: Calls the storage provider to produce a time-limited upload URL.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.DTOs.StorageObject.Validators;
using Explore.Application.Features.StorageObjects.Requests.Commands;
using FluentValidation;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Handlers.Commands;

public class GenerateUploadUrlCommandHandler : IRequestHandler<GenerateUploadUrlCommand, UploadUrlResponseDto>
{
    private readonly IObjectStorageService _objectStorageService;

    public GenerateUploadUrlCommandHandler(IObjectStorageService objectStorageService)
    {
        _objectStorageService = objectStorageService;
    }

    public async Task<UploadUrlResponseDto> Handle(GenerateUploadUrlCommand request, CancellationToken cancellationToken)
    {
        var uploadRequest = new UploadRequestDto
        {
            FileName = UploadRequestDtoValidator.NormalizeFileName(request.FileName),
            ContentType = UploadRequestDtoValidator.NormalizeContentType(request.ContentType)
        };

        var validator = new UploadRequestDtoValidator();
        var validationResult = await validator.ValidateAsync(uploadRequest, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var response = await _objectStorageService.GeneratePresignedUploadUrl(
            uploadRequest.FileName,
            uploadRequest.ContentType);
        return response;
    }
}
