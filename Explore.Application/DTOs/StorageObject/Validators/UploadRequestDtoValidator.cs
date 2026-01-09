using FluentValidation;

namespace Explore.Application.DTOs.StorageObject.Validators
{
    public class UploadRequestDtoValidator : AbstractValidator<UploadRequestDto>
    {
        public UploadRequestDtoValidator()
        {
            RuleFor(x => x.FileName)
                .NotEmpty().WithMessage("{PropertyName} is required")
                .MaximumLength(500).WithMessage("{PropertyName} must not exceed 500 characters");

            RuleFor(x => x.ContentType)
                .NotEmpty().WithMessage("{PropertyName} is required")
                .MaximumLength(100).WithMessage("{PropertyName} must not exceed 100 characters")
                .Must(BeValidContentType).WithMessage("{PropertyName} must be a valid MIME type");
        }

        private bool BeValidContentType(string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType)) return false;
            // Basic MIME type validation (type/subtype)
            return contentType.Contains("/") && contentType.Split('/').Length == 2;
        }
    }
}
