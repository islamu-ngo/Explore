// ABOUTME: MediatR command for updating a tag-to-tag-type link.
// ABOUTME: Carries server-owned junction identity and grouped relationship changes.
using Explore.Application.DTOs.TagTypeTags;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.TagTypeTags.Requests.Commands;

public class UpdateTagTypeTagsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid TagTypeTagsId { get; set; }
    public required UpdateTagTypeTagsDto TagTypeTagsDto { get; set; }
}
