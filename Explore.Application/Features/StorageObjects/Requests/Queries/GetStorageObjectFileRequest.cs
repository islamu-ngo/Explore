using System.IO;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Requests.Queries;

public class GetStorageObjectFileRequest : IRequest<(Stream FileStream, string ContentType)>
{
    public required string FileKey { get; set; } = string.Empty;
}
