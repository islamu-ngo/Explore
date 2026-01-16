using MediatR;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Responses;

namespace Explore.Application.Features.StorageObjects.Requests.Commands
{
    public class UpdateStorageObjectCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public UpdateStorageObjectDto StorageObjectDto { get; set; }
    }
}
