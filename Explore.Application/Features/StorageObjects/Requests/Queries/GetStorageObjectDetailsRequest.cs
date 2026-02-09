using Explore.Application.DTOs.StorageObject;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Requests.Queries;

public class GetStorageObjectDetailsRequest : IRequest<StorageObjectDto?>
{
    public Guid Id { get; set; }
}
