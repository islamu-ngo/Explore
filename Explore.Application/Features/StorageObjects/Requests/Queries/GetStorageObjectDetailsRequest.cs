using MediatR;
using Explore.Application.DTOs.StorageObject;

namespace Explore.Application.Features.StorageObjects.Requests.Queries
{
    public class GetStorageObjectDetailsRequest : IRequest<StorageObjectDto?>
    {
        public Guid Id { get; set; }
    }
}
