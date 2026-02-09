using Explore.Application.DTOs.StorageObject;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Requests.Commands;

public class CreateStorageObjectCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required CreateStorageObjectDto StorageObjectDto { get; set; }
}
