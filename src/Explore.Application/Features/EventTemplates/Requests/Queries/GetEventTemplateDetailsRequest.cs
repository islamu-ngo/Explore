// ABOUTME: Query request for retrieving one event template with all nested definitions and options.
// ABOUTME: Used by tenant-admin detail views for template configuration management.

using Explore.Application.DTOs.EventTemplate;
using MediatR;

namespace Explore.Application.Features.EventTemplates.Requests.Queries;

public sealed record GetEventTemplateDetailsRequest(Guid Id = default) : IRequest<EventTemplateDto>;
