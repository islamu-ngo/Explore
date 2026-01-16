using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.StorageObjects.Requests.Queries;
using MediatR;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Explore.Application.Features.StorageObjects.Handlers.Queries;

public class GetStorageObjectFileRequestHandler : IRequestHandler<GetStorageObjectFileRequest, (Stream FileStream, string ContentType)>
{
    private readonly IObjectStorageService _objectStorageService;

    public GetStorageObjectFileRequestHandler(IObjectStorageService objectStorageService)
    {
        _objectStorageService = objectStorageService;
    }

    public async Task<(Stream FileStream, string ContentType)> Handle(GetStorageObjectFileRequest request, CancellationToken cancellationToken)
    {
        return await _objectStorageService.GetFileStream(request.FileKey);
    }
}
