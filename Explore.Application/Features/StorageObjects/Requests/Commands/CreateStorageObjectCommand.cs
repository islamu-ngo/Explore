using MediatR;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Responses;

namespace Explore.Application.Features.StorageObjects.Requests.Commands
{
    public class CreateStorageObjectCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public CreateStorageObjectDto StorageObjectDto { get; set; }
    }
}
