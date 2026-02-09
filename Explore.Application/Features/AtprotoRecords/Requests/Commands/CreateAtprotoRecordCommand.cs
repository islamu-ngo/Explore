using Explore.Application.DTOs.AtprotoRecord;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.AtprotoRecords.Requests.Commands;

public class CreateAtprotoRecordCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required CreateAtprotoRecordDto AtprotoRecordDto { get; set; }
}
