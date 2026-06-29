// ABOUTME: FluentValidation validator for grouped UpdateEventRegistrationDto PATCH payloads.
// ABOUTME: Enforces wrapper presence and explicit clear semantics for nullable registration groups.

using FluentValidation;

namespace Explore.Application.DTOs.EventRegistration.Validators;

public class UpdateEventRegistrationDtoValidator : AbstractValidator<UpdateEventRegistrationDto>
{
    public UpdateEventRegistrationDtoValidator()
    {
        RuleFor(dto => dto)
            .Must(HaveAtLeastOneGroup)
            .WithMessage("At least one event registration update group must be provided.");

        When(dto => dto.User is not null, () =>
        {
            RuleFor(dto => dto.User!.UserId)
                .NotEmpty().WithMessage("UserId is required.");
        });

        When(dto => dto.Session is not null, () =>
        {
            RuleFor(dto => dto.Session!.EventSessionId)
                .NotEmpty().WithMessage("EventSessionId is required.");
        });

        When(dto => dto.Intent is not null, () =>
        {
            RuleFor(dto => dto.Intent!.EventRegistrationIntentId.HasValue)
                .Equal(true)
                .WithMessage("EventRegistrationIntentId must specify an explicit field operation.");
        });

        When(dto => dto.ApprovalStatus is not null, () =>
        {
            RuleFor(dto => dto.ApprovalStatus!.ApprovalStatusId.HasValue)
                .Equal(true)
                .WithMessage("ApprovalStatusId must specify an explicit field operation.");
        });

        When(dto => dto.AtprotoRecord is not null, () =>
        {
            RuleFor(dto => dto.AtprotoRecord!.AtprotoRecordId.HasValue)
                .Equal(true)
                .WithMessage("AtprotoRecordId must specify an explicit field operation.");
        });
    }

    private static bool HaveAtLeastOneGroup(UpdateEventRegistrationDto dto)
    {
        return dto.User is not null
            || dto.Session is not null
            || dto.Intent is not null
            || dto.ApprovalStatus is not null
            || dto.AtprotoRecord is not null;
    }
}
