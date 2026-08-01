// ABOUTME: Validates participant, concrete ordinal, bulk payload, and explicit UTC deadline command shapes.
// ABOUTME: Leaves order ownership, catalog mode, guardian, and eligibility checks to transaction-bound handlers.

using Explore.Application.Features.RegistrationOrders.Requests.Commands;
using FluentValidation;

namespace Explore.Application.Features.RegistrationOrders.Validators;

public sealed class AddRegistrationParticipantCommandValidator : AbstractValidator<AddRegistrationParticipantCommand>
{
    public AddRegistrationParticipantCommandValidator()
    {
        RuleFor(x => x.RegistrationOrderId).NotEmpty();
        RuleFor(x => x.ParticipantTypeId).GreaterThan(0);
        RuleFor(x => x.Details).NotNull();
    }
}

public sealed class UpdateRegistrationParticipantCommandValidator : AbstractValidator<UpdateRegistrationParticipantCommand>
{
    public UpdateRegistrationParticipantCommandValidator()
    {
        RuleFor(x => x.RegistrationOrderId).NotEmpty();
        RuleFor(x => x.ParticipantId).NotEmpty();
        RuleFor(x => x.ParticipantTypeId).GreaterThan(0);
        RuleFor(x => x.Details).NotNull();
    }
}

public sealed class AssignRegistrationTicketCommandValidator : AbstractValidator<AssignRegistrationTicketCommand>
{
    public AssignRegistrationTicketCommandValidator()
    {
        RuleFor(x => x.RegistrationOrderId).NotEmpty();
        RuleFor(x => x.RegistrationOrderLineId).NotEmpty();
        RuleFor(x => x.Ordinal).GreaterThan(0);
        RuleFor(x => x.ParticipantId).NotEmpty();
    }
}

public sealed class BulkAssignRegistrationTicketsCommandValidator : AbstractValidator<BulkAssignRegistrationTicketsCommand>
{
    public BulkAssignRegistrationTicketsCommandValidator()
    {
        RuleFor(x => x.RegistrationOrderId).NotEmpty();
        RuleFor(x => x.Assignments).NotNull().NotEmpty();
        RuleForEach(x => x.Assignments).SetValidator(new TicketParticipantAssignmentInputDtoValidator());
    }
}

public sealed class DeferRegistrationTicketCommandValidator : AbstractValidator<DeferRegistrationTicketCommand>
{
    public DeferRegistrationTicketCommandValidator()
    {
        RuleFor(x => x.RegistrationOrderId).NotEmpty();
        RuleFor(x => x.RegistrationOrderLineId).NotEmpty();
        RuleFor(x => x.Ordinal).GreaterThan(0);
        RuleFor(x => x.AssignmentDeadline).Must(value => value.Kind == DateTimeKind.Utc);
    }
}

public sealed class BulkDeferRegistrationTicketsCommandValidator : AbstractValidator<BulkDeferRegistrationTicketsCommand>
{
    public BulkDeferRegistrationTicketsCommandValidator()
    {
        RuleFor(x => x.RegistrationOrderId).NotEmpty();
        RuleFor(x => x.Assignments).NotNull().NotEmpty();
        RuleForEach(x => x.Assignments).SetValidator(new TicketDeferralInputDtoValidator());
        RuleFor(x => x.AssignmentDeadline).Must(value => value.Kind == DateTimeKind.Utc);
    }
}

internal sealed class TicketParticipantAssignmentInputDtoValidator : AbstractValidator<DTOs.RegistrationOrders.TicketParticipantAssignmentInputDto>
{
    public TicketParticipantAssignmentInputDtoValidator()
    {
        RuleFor(x => x.RegistrationOrderLineId).NotEmpty();
        RuleFor(x => x.Ordinal).GreaterThan(0);
        RuleFor(x => x.ParticipantId).NotEmpty();
    }
}

internal sealed class TicketDeferralInputDtoValidator : AbstractValidator<DTOs.RegistrationOrders.TicketDeferralInputDto>
{
    public TicketDeferralInputDtoValidator()
    {
        RuleFor(x => x.RegistrationOrderLineId).NotEmpty();
        RuleFor(x => x.Ordinal).GreaterThan(0);
    }
}
