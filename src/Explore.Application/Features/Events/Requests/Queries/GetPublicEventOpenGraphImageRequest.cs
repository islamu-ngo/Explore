// ABOUTME: Query request for rendering a public event Open Graph image by its public slug-code.
// ABOUTME: Keeps the public URL input within the Events CQRS request structure.

using Explore.Application.Features.Events.OpenGraph;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Queries;

public sealed record GetPublicEventOpenGraphImageRequest : IRequest<EventOpenGraphImageRenderResult?>
{
    public required string SlugCode { get; init; }
}
