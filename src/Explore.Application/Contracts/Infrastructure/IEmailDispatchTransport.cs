// ABOUTME: Application boundary for optional EmailDispatch broker transports.
// ABOUTME: Keeps RabbitMQ reliability semantics out of handlers, controllers, and generic messaging abstractions.

namespace Explore.Application.Contracts.Infrastructure;

public interface IEmailDispatchTransport
{
    Task DeclareTopologyAsync(CancellationToken cancellationToken = default);

    Task<EmailDispatchPublishResult> PublishDispatchPointerAsync(
        EmailDispatchPointer pointer,
        CancellationToken cancellationToken = default);

    Task<EmailDispatchTransportHealth> CheckHealthAsync(CancellationToken cancellationToken = default);
}
