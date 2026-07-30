// ABOUTME: Validates the aggregate identifier shared by registration-order lifecycle commands.
// ABOUTME: Is manually instantiated by each handler to preserve the Application validation contract.

using Explore.Application.Features.RegistrationOrders.Requests.Commands;
using FluentValidation;

namespace Explore.Application.Features.RegistrationOrders.Validators;

public sealed class RegistrationOrderLifecycleCommandValidator<TCommand> : AbstractValidator<TCommand>
    where TCommand : IRegistrationOrderLifecycleCommand
{
    public RegistrationOrderLifecycleCommandValidator()
    {
        RuleFor(command => command.OrderId).NotEmpty();
    }
}
