// ABOUTME: MediatR query for fetching tags on a given event.
// ABOUTME: Returns IEnumerable<TagDto>.
using System;
using System.Collections.Generic;
using Explore.Application.DTOs.Tag;
using MediatR;

namespace Explore.Application.Features.EventTags.Requests.Queries;

public sealed record GetTagsByEventRequest(Guid EventId = default) : IRequest<List<TagListDto>>;
