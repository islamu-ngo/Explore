using Explore.Application.DTOs.AtprotoRecord;
using MediatR;

namespace Explore.Application.Features.AtprotoRecords.Requests.Queries;

public class GetAtprotoRecordListRequest : IRequest<List<AtprotoRecordListDto>>
{
}
