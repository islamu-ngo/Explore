// ABOUTME: MediatR command for creating a tag-to-tag-type link.
// ABOUTME: Carries the CreateTagTypeTagsDto payload.
using Explore.Application.DTOs.TagTypeTags;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.TagTypeTags.Requests.Commands;

public sealed record CreateTagTypeTagsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required CreateTagTypeTagsDto TagTypeTagsDto { get; init; }
}
