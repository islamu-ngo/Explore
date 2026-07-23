// ABOUTME: Portable Application port and models for public event Open Graph image rendering.
// ABOUTME: Deliberately excludes renderer-library types so API-local implementations can choose their renderer later.

using Explore.Application.Features.Events.Requests.Queries;

namespace Explore.Application.Features.Events.OpenGraph;

public interface IEventOpenGraphImageRenderer
{
    Task<EventOpenGraphImageRenderResult> RenderAsync(
        EventOpenGraphImageRenderRequest request,
        CancellationToken cancellationToken);
}

public sealed record EventOpenGraphImageRenderResult(byte[] PngBytes, string ETag);
