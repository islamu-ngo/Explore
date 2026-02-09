using MediatR;

namespace Explore.Application.Features.TagTypeTags.Requests.Commands;

public class DeleteTagTypeTagsCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
