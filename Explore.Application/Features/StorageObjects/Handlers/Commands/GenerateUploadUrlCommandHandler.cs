using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.StorageObjects.Requests.Commands;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Handlers.Commands
{
    public class GenerateUploadUrlCommandHandler : IRequestHandler<GenerateUploadUrlCommand, string>
    {
        private readonly IObjectStorageService _objectStorageService; 

        public GenerateUploadUrlCommandHandler(IObjectStorageService objectStorageService)
        {
            _objectStorageService = objectStorageService;
        }

        public async Task<string> Handle(GenerateUploadUrlCommand request, CancellationToken cancellationToken)
        {
            var uploadUrl = await _objectStorageService.GeneratePresignedUploadUrl(request.FileName, request.ContentType);
            return uploadUrl;
        }
    }
}
