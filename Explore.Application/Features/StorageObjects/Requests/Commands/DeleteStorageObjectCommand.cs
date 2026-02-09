using MediatR;

namespace Explore.Application.Features.StorageObjects.Requests.Commands;

public class DeleteStorageObjectCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
