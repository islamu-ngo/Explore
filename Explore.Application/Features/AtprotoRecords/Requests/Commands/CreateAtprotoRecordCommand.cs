using MediatR;
using Explore.Application.DTOs.AtprotoRecord;
using Explore.Application.Responses;

namespace Explore.Application.Features.AtprotoRecords.Requests.Commands
{
    public class CreateAtprotoRecordCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public CreateAtprotoRecordDto AtprotoRecordDto { get; set; }
    }
}
