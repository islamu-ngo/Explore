// ABOUTME: Requests one tenant-admin evidence-based resolution of an ambiguous IntegrationSync outcome.
// ABOUTME: Never retries unless the operator explicitly proves the provider did not accept the request.

using Explore.Application.DTOs.Integrations;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Integrations.Listmonk.Requests.Commands;

public sealed record ResolveIntegrationSyncAmbiguityCommand(
    Guid OutboxId,
    ResolveIntegrationSyncAmbiguityDto Resolution) : IRequest<BaseCommandResponse<Guid>>;
