// ABOUTME: Command for revoking a persisted external API key.
// ABOUTME: Lets handlers hide non-visible keys behind the same false result used for missing records.

using MediatR;

namespace Explore.Application.Features.ExternalApiKeys.Requests.Commands;

public class RevokeExternalApiKeyCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
