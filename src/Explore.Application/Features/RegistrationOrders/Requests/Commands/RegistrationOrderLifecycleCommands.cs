// ABOUTME: Defines lifecycle commands for submitting, routing, finalizing, and resolving registration orders.
// ABOUTME: Commands carry only an aggregate identifier; authorization remains a separate policy concern.

using Explore.Application.DTOs.RegistrationOrders;
using MediatR;

namespace Explore.Application.Features.RegistrationOrders.Requests.Commands;

public interface IRegistrationOrderLifecycleCommand
{
    Guid OrderId { get; }
}

public sealed record SubmitRegistrationOrderCommand(Guid OrderId)
    : IRequest<RegistrationOrderLifecycleResponse>, IRegistrationOrderLifecycleCommand;

public sealed record ReadyRegistrationOrderForCheckoutCommand(Guid OrderId)
    : IRequest<RegistrationOrderLifecycleResponse>, IRegistrationOrderLifecycleCommand;

public sealed record FinalizeFreeRegistrationOrderCommand(Guid OrderId)
    : IRequest<RegistrationOrderLifecycleResponse>, IRegistrationOrderLifecycleCommand;

public sealed record CancelRegistrationOrderCommand(Guid OrderId)
    : IRequest<RegistrationOrderLifecycleResponse>, IRegistrationOrderLifecycleCommand;

public sealed record ApproveRegistrationOrderCommand(Guid OrderId)
    : IRequest<RegistrationOrderLifecycleResponse>, IRegistrationOrderLifecycleCommand;

public sealed record RejectRegistrationOrderCommand(Guid OrderId)
    : IRequest<RegistrationOrderLifecycleResponse>, IRegistrationOrderLifecycleCommand;
