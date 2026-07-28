// ABOUTME: Command for updating editable policy fields on a persisted external API key.
// ABOUTME: Keeps ownership immutable and routes policy maintenance through the application layer.

using Explore.Application.DTOs.ExternalApiKey;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ExternalApiKeys.Requests.Commands;

public class UpdateExternalApiKeyPolicyCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid ExternalApiKeyId { get; init; }
    public required UpdateExternalApiKeyPolicyDto ExternalApiKeyPolicyDto { get; set; }
}
