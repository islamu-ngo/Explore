// ABOUTME: Validates private AI assistant send-message requests before provider orchestration.
// ABOUTME: Keeps prompt-sized user input and idempotency keys bounded at the Application boundary.

using Explore.Application.DTOs.Ai;
using FluentValidation;

namespace Explore.Application.DTOs.Ai.Validators;

public sealed class SendAiMessageRequestDtoValidator : AbstractValidator<SendAiMessageRequestDto>
{
    private const int MaxContentLength = 16_000;
    private const int MaxMediaTypeLength = 128;
    private const int MaxFileNameLength = 255;
    private const int MaxBase64Characters = ((AiMessageImageAttachmentSerializer.MaxImageBytes + 2) / 3) * 4;

    public SendAiMessageRequestDtoValidator()
    {
        RuleFor(request => request.Content)
            .MaximumLength(MaxContentLength);

        RuleFor(request => request)
            .Must(request => !string.IsNullOrWhiteSpace(request.Content) || (request.Images?.Count ?? 0) > 0)
            .WithMessage("AI messages require content or at least one image.");

        RuleFor(request => request.Images)
            .Must(images => (images?.Count ?? 0) <= AiMessageImageAttachmentSerializer.MaxImageCount)
            .WithMessage($"AI messages can include at most {AiMessageImageAttachmentSerializer.MaxImageCount} images.");

        RuleForEach(request => request.Images)
            .ChildRules(image =>
            {
                image.RuleFor(value => value.MediaType)
                    .NotEmpty()
                    .MaximumLength(MaxMediaTypeLength)
                    .Must(IsImageMediaType)
                    .WithMessage("AI message image media type must start with 'image/'.");

                image.RuleFor(value => value.Data)
                    .NotEmpty()
                    .Must(BePlainBase64ImageData)
                    .WithMessage("AI message image data must be plain base64 text within the allowed image size.");

                image.RuleFor(value => value.FileName)
                    .MaximumLength(MaxFileNameLength)
                    .When(value => !string.IsNullOrWhiteSpace(value.FileName));

                image.RuleFor(value => value.SizeBytes)
                    .GreaterThan(0)
                    .LessThanOrEqualTo(AiMessageImageAttachmentSerializer.MaxImageBytes)
                    .When(value => value.SizeBytes.HasValue);
            });

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

    private static bool IsImageMediaType(string? mediaType) =>
        !string.IsNullOrWhiteSpace(mediaType)
        && mediaType.Trim().StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    private static bool BePlainBase64ImageData(string? data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return false;
        }

        var value = data.Trim();
        if (value.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            || value.Length > MaxBase64Characters)
        {
            return false;
        }

        try
        {
            return Convert.FromBase64String(value).LongLength <= AiMessageImageAttachmentSerializer.MaxImageBytes;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
