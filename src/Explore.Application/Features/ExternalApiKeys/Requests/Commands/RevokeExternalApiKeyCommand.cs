// ABOUTME: Command for revoking a persisted external API key.
// ABOUTME: Lets handlers hide non-visible keys behind the same false result used for missing records.

using MediatR;

namespace Explore.Application.Features.ExternalApiKeys.Requests.Commands;

public sealed record RevokeExternalApiKeyCommand(Guid Id = default) : IRequest<bool>;
