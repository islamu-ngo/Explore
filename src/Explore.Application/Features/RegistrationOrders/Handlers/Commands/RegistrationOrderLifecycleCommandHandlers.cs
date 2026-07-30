// ABOUTME: Handles one registration-order lifecycle command per handler with manual validation.
// ABOUTME: Delegates transaction-sensitive state changes to the order lifecycle service.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.DTOs.RegistrationOrders;
using Explore.Application.Features.RegistrationOrders.Requests.Commands;
using Explore.Application.Features.RegistrationOrders.Validators;
using Explore.Application.Services.Registration;
using MediatR;

namespace Explore.Application.Features.RegistrationOrders.Handlers.Commands;

public sealed class SubmitRegistrationOrderCommandHandler(
    RegistrationOrderLifecycleService lifecycle,
    ITenantContext tenant)
    : IRequestHandler<SubmitRegistrationOrderCommand, RegistrationOrderLifecycleResponse>
{
    public async Task<RegistrationOrderLifecycleResponse> Handle(SubmitRegistrationOrderCommand request, CancellationToken cancellationToken)
    {
        var validator = new RegistrationOrderLifecycleCommandValidator<SubmitRegistrationOrderCommand>();
        var validation = await validator.ValidateAsync(request, cancellationToken);
        RegistrationOrderLifecycleResponse? failure = RegistrationOrderLifecycleCommandFailures.Failure(request, validation);
        return failure ?? await lifecycle.SubmitAsync(request.OrderId, tenant.TenantId, cancellationToken);
    }
}

public sealed class ReadyRegistrationOrderForCheckoutCommandHandler(
    RegistrationOrderLifecycleService lifecycle,
    ITenantContext tenant)
    : IRequestHandler<ReadyRegistrationOrderForCheckoutCommand, RegistrationOrderLifecycleResponse>
{
    public async Task<RegistrationOrderLifecycleResponse> Handle(ReadyRegistrationOrderForCheckoutCommand request, CancellationToken cancellationToken)
    {
        var validator = new RegistrationOrderLifecycleCommandValidator<ReadyRegistrationOrderForCheckoutCommand>();
        var validation = await validator.ValidateAsync(request, cancellationToken);
        RegistrationOrderLifecycleResponse? failure = RegistrationOrderLifecycleCommandFailures.Failure(request, validation);
        return failure ?? await lifecycle.ReadyForCheckoutAsync(request.OrderId, tenant.TenantId, cancellationToken);
    }
}

public sealed class FinalizeFreeRegistrationOrderCommandHandler(
    RegistrationOrderLifecycleService lifecycle,
    ITenantContext tenant)
    : IRequestHandler<FinalizeFreeRegistrationOrderCommand, RegistrationOrderLifecycleResponse>
{
    public async Task<RegistrationOrderLifecycleResponse> Handle(FinalizeFreeRegistrationOrderCommand request, CancellationToken cancellationToken)
    {
        var validator = new RegistrationOrderLifecycleCommandValidator<FinalizeFreeRegistrationOrderCommand>();
        var validation = await validator.ValidateAsync(request, cancellationToken);
        RegistrationOrderLifecycleResponse? failure = RegistrationOrderLifecycleCommandFailures.Failure(request, validation);
        return failure ?? await lifecycle.FinalizeFreeAsync(request.OrderId, tenant.TenantId, cancellationToken);
    }
}

public sealed class CancelRegistrationOrderCommandHandler(
    RegistrationOrderLifecycleService lifecycle,
    ITenantContext tenant)
    : IRequestHandler<CancelRegistrationOrderCommand, RegistrationOrderLifecycleResponse>
{
    public async Task<RegistrationOrderLifecycleResponse> Handle(CancelRegistrationOrderCommand request, CancellationToken cancellationToken)
    {
        var validator = new RegistrationOrderLifecycleCommandValidator<CancelRegistrationOrderCommand>();
        var validation = await validator.ValidateAsync(request, cancellationToken);
        RegistrationOrderLifecycleResponse? failure = RegistrationOrderLifecycleCommandFailures.Failure(request, validation);
        return failure ?? await lifecycle.CancelAsync(request.OrderId, tenant.TenantId, cancellationToken);
    }
}

public sealed class ApproveRegistrationOrderCommandHandler(
    RegistrationOrderLifecycleService lifecycle,
    ITenantContext tenant)
    : IRequestHandler<ApproveRegistrationOrderCommand, RegistrationOrderLifecycleResponse>
{
    public async Task<RegistrationOrderLifecycleResponse> Handle(ApproveRegistrationOrderCommand request, CancellationToken cancellationToken)
    {
        var validator = new RegistrationOrderLifecycleCommandValidator<ApproveRegistrationOrderCommand>();
        var validation = await validator.ValidateAsync(request, cancellationToken);
        RegistrationOrderLifecycleResponse? failure = RegistrationOrderLifecycleCommandFailures.Failure(request, validation);
        return failure ?? await lifecycle.ApproveAsync(request.OrderId, tenant.TenantId, cancellationToken);
    }
}

public sealed class RejectRegistrationOrderCommandHandler(
    RegistrationOrderLifecycleService lifecycle,
    ITenantContext tenant)
    : IRequestHandler<RejectRegistrationOrderCommand, RegistrationOrderLifecycleResponse>
{
    public async Task<RegistrationOrderLifecycleResponse> Handle(RejectRegistrationOrderCommand request, CancellationToken cancellationToken)
    {
        var validator = new RegistrationOrderLifecycleCommandValidator<RejectRegistrationOrderCommand>();
        var validation = await validator.ValidateAsync(request, cancellationToken);
        RegistrationOrderLifecycleResponse? failure = RegistrationOrderLifecycleCommandFailures.Failure(request, validation);
        return failure ?? await lifecycle.RejectAsync(request.OrderId, tenant.TenantId, cancellationToken);
    }
}

file static class RegistrationOrderLifecycleCommandFailures
{
    public static RegistrationOrderLifecycleResponse? Failure<TCommand>(
        TCommand command,
        FluentValidation.Results.ValidationResult validation)
        where TCommand : IRegistrationOrderLifecycleCommand
    {
        return validation.IsValid
            ? null
            : new RegistrationOrderLifecycleResponse
            {
                Id = command.OrderId,
                Success = false,
                Message = "Registration order lifecycle request is invalid.",
                Errors = validation.Errors.Select(error => error.ErrorMessage).ToList()
            };
    }
}
