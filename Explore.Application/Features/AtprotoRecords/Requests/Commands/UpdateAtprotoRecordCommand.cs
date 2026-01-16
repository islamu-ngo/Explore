using MediatR;
using Explore.Application.DTOs.AtprotoRecord;
using Explore.Application.Responses;

namespace Explore.Application.Features.AtprotoRecords.Requests.Commands
{
    public class UpdateAtprotoRecordCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public UpdateAtprotoRecordDto AtprotoRecordDto { get; set; }
    }
}
