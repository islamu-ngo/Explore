using Explore.Application.DTOs.StorageObject;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Requests.Commands;

public class UpdateStorageObjectCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required UpdateStorageObjectDto StorageObjectDto { get; set; }
}
