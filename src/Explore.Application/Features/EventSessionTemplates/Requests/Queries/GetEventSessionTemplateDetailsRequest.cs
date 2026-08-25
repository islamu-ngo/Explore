// ABOUTME: Query request for retrieving one event session template with all nested definitions and options.
// ABOUTME: Used by tenant-admin detail views for session template configuration management.

using Explore.Application.DTOs.EventSessionTemplate;
using MediatR;

namespace Explore.Application.Features.EventSessionTemplates.Requests.Queries;

public sealed record GetEventSessionTemplateDetailsRequest(Guid Id = default) : IRequest<EventSessionTemplateDto>;
