// ABOUTME: MediatR query request for fetching a single tag by ID.
// ABOUTME: Returns TagDto.
using System;
using Explore.Application.DTOs.Tag;
using MediatR;

namespace Explore.Application.Features.Tags.Requests.Queries;

public sealed record GetTagDetailsRequest(Guid Id = default) : IRequest<TagDto>;
