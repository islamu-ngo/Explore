using MediatR;
using Explore.Application.DTOs.AtprotoRecord;

namespace Explore.Application.Features.AtprotoRecords.Requests.Queries
{
    public class GetAtprotoRecordListRequest : IRequest<List<AtprotoRecordListDto>>
    {
    }
}
