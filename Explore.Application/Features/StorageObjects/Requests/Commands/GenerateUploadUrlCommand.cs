using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.DTOs.StorageObject;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Requests.Commands;

public class GenerateUploadUrlCommand : IRequest<UploadUrlResponseDto>
{
    public required string FileName { get; set; } = string.Empty;
    public required string ContentType { get; set; } = string.Empty;
}
