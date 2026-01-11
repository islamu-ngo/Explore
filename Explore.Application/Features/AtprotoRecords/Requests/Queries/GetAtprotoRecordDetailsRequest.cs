using MediatR;
using Explore.Application.DTOs.AtprotoRecord;

namespace Explore.Application.Features.AtprotoRecords.Requests.Queries
{
    public class GetAtprotoRecordDetailsRequest : IRequest<AtprotoRecordDto?>
    {
        public Guid Id { get; set; }
    }
}
