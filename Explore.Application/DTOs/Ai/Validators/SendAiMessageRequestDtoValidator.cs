// ABOUTME: Validates private AI assistant send-message requests before provider orchestration.
// ABOUTME: Keeps prompt-sized user input and idempotency keys bounded at the Application boundary.

using Explore.Application.DTOs.Ai;
using FluentValidation;

namespace Explore.Application.DTOs.Ai.Validators;

public sealed class SendAiMessageRequestDtoValidator : AbstractValidator<SendAiMessageRequestDto>
{
    public SendAiMessageRequestDtoValidator()
    {
        RuleFor(request => request.Content)
            .NotEmpty()
            .MaximumLength(16_000);

        RuleFor(request => request.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(request => request.ModelId)
            .MaximumLength(256)
            .When(request => !string.IsNullOrWhiteSpace(request.ModelId));

        RuleFor(request => request.Mode)
            .MaximumLength(16)
            .Must(AiAssistantInteractionModes.IsValid)
            .WithMessage("AI assistant mode must be 'ask' or 'build'.");
    }
}
