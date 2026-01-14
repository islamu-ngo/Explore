using MediatR;
using System.IO;

namespace Explore.Application.Features.StorageObjects.Requests.Queries;

public class GetStorageObjectFileRequest : IRequest<(Stream FileStream, string ContentType)>
{
    public string FileKey { get; set; } = string.Empty;
}
