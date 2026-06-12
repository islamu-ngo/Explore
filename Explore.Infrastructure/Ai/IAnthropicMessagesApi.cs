// ABOUTME: Refit interface for the Anthropic Messages API with multi-turn tool calling support.
// ABOUTME: Supports dynamic endpoint resolution for multi-tenant governance per-request routing.

using Refit;

namespace Explore.Infrastructure.Ai;

[Headers("Content-Type: application/json", "anthropic-version: 2023-06-01")]
public interface IAnthropicMessagesApi
{
    [Post("/messages")]
    Task<ApiResponse<AnthropicMessageResponse>> CreateMessageAsync(
        [Body] AnthropicCreateMessageRequest request,
        [Header("x-api-key")] string? apiKey,
        CancellationToken cancellationToken = default);
}
