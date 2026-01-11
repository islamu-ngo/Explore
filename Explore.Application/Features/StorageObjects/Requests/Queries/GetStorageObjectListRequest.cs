using MediatR;
using Explore.Application.DTOs.StorageObject;

namespace Explore.Application.Features.StorageObjects.Requests.Queries
{
    public class GetStorageObjectListRequest : IRequest<List<StorageObjectListDto>>
    {
    }
}
