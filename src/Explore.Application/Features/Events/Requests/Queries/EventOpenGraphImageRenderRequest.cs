// ABOUTME: Portable render input for a public event Open Graph image.
// ABOUTME: Keeps query-shaped render data in the Events Requests.Queries namespace required by CQRS conventions.

namespace Explore.Application.Features.Events.Requests.Queries;

public sealed record EventOpenGraphImageRenderRequest(
    string Title,
    DateOnly? FirstSessionDate,
    DateOnly? LastSessionDate,
    string BrandDisplayName,
    Stream? FeaturedImage,
    string? FeaturedImageContentType);
