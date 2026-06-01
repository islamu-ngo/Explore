// ABOUTME: Provider-neutral AI chat contract used by Application handlers without referencing provider SDK types.
// ABOUTME: Infrastructure implementations adapt OpenAI-compatible, fake, or future providers behind this boundary.

namespace Explore.Application.Contracts.Infrastructure.Ai;

public interface IAiChatProvider
{
    Task<AiChatProviderResult> SendAsync(AiChatPayload request, CancellationToken cancellationToken = default);
}
