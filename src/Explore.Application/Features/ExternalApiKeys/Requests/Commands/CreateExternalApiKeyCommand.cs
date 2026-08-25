// ABOUTME: Command for issuing a new persisted external API key.
// ABOUTME: Wraps the creation DTO so the handler can enforce tenant and owner context centrally.

using Explore.Application.DTOs.ExternalApiKey;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ExternalApiKeys.Requests.Commands;

public sealed record CreateExternalApiKeyCommand : IRequest<CreateExternalApiKeyCommandResponse>
{
    public required CreateExternalApiKeyDto ExternalApiKeyDto { get; init; }
}
