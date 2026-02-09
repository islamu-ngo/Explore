using MediatR;

namespace Explore.Application.Features.AtprotoRecords.Requests.Commands;

public class DeleteAtprotoRecordCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
