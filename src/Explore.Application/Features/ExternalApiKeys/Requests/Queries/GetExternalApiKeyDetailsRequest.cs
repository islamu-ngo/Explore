// ABOUTME: Query for fetching a single visible external API key without exposing secret material.
// ABOUTME: Returns null for missing or unauthorized keys so the API can fail closed with not found.

using Explore.Application.DTOs.ExternalApiKey;
using MediatR;

namespace Explore.Application.Features.ExternalApiKeys.Requests.Queries;

public sealed record GetExternalApiKeyDetailsRequest(Guid Id = default) : IRequest<ExternalApiKeyListDto?>;
