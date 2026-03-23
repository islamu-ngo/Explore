// ABOUTME: MediatR query for downloading the binary content of a storage object.
// ABOUTME: Returns file bytes and content type.
using System.IO;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Requests.Queries;

public class GetStorageObjectFileRequest : IRequest<(Stream FileStream, string ContentType)>
{
    public required string FileKey { get; set; } = string.Empty;
}
