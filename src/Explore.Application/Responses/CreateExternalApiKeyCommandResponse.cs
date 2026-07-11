// ABOUTME: Command response for external API key creation.
// ABOUTME: Carries the one-time reveal secret alongside the normal command status envelope.

namespace Explore.Application.Responses;

public class CreateExternalApiKeyCommandResponse : BaseCommandResponse<Guid>
{
    public string? ApiKey { get; set; }
    public string? KeyId { get; set; }
}
