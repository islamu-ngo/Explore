// ABOUTME: Validator for creating a private AI assistant conversation shell.
// ABOUTME: Enforces bounded user-supplied metadata before persistence handlers run.

using FluentValidation;

namespace Explore.Application.DTOs.Ai.Validators;

public sealed class CreateAiConversationRequestDtoValidator : AbstractValidator<CreateAiConversationRequestDto>
{
    public CreateAiConversationRequestDtoValidator()
    {
        RuleFor(dto => dto.Title)
            .MaximumLength(200)
            .When(dto => !string.IsNullOrWhiteSpace(dto.Title));

        RuleFor(dto => dto.ActorId)
            .Must(actorId => actorId is null || actorId != Guid.Empty)
            .WithMessage("AI acting actor id must be a non-empty actor identifier.");
    }
}
