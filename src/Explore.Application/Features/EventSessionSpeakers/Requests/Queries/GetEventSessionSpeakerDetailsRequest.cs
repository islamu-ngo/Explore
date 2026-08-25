// ABOUTME: MediatR query request for fetching a single session-speaker link by ID.
// ABOUTME: Returns EventSessionSpeakerDto.
using System;
using Explore.Application.DTOs.EventSessionSpeaker;
using MediatR;

namespace Explore.Application.Features.EventSessionSpeakers.Requests.Queries;

public sealed record GetEventSessionSpeakerDetailsRequest(Guid Id = default) : IRequest<EventSessionSpeakerDto>;
