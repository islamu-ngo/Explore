// ABOUTME: MediatR command for deleting a tag-to-tag-type link by ID.
// ABOUTME: Carries the target junction record ID.
using MediatR;

namespace Explore.Application.Features.TagTypeTags.Requests.Commands;

public class DeleteTagTypeTagsCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
