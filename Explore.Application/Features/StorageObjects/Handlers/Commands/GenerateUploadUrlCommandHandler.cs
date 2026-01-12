using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Features.StorageObjects.Requests.Commands;
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
        var response = await _objectStorageService.GeneratePresignedUploadUrl(request.FileName, request.ContentType);
        return response;
    }
}
