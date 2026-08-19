// ABOUTME: Command to update a footer link's label, URL, and display options.
// ABOUTME: Validates the link's parent group belongs to the current tenant.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using Explore.Application.Validation;
using FluentValidation;
using MediatR;

namespace Explore.Application.Features.Footer.Requests.Commands;

[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]
public class UpdateFooterLinkCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TenantId { get; set; }
    public Guid LinkId { get; set; }
    public required PatchFooterLinkDto Update { get; set; }
    string? ISecureRequest.ResourceId => TenantId == Guid.Empty ? null : TenantId.ToString("D");
    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        TenantId == Guid.Empty
        ? null
        : new TenantScopedAuthorizationFacts(TenantId);

}

public sealed class PatchFooterLinkDto
{
    public PatchFooterLinkLabelDto? Label { get; set; }
    public PatchFooterLinkUrlDto? Url { get; set; }
    public PatchFooterLinkOpenInNewTabDto? OpenInNewTab { get; set; }
    public PatchFooterLinkIsActiveDto? IsActive { get; set; }
}

public sealed class PatchFooterLinkLabelDto
{
    public required string Value { get; set; }
}

public sealed class PatchFooterLinkUrlDto
{
    public required string Value { get; set; }
}

public sealed class PatchFooterLinkOpenInNewTabDto
{
    public bool? Value { get; set; }
}

public sealed class PatchFooterLinkIsActiveDto
{
    public bool? Value { get; set; }
}

public sealed class PatchFooterLinkDtoValidator : AbstractValidator<PatchFooterLinkDto>
{
    public PatchFooterLinkDtoValidator(bool requireHttps = true)
    {
        RuleFor(dto => dto.Label!.Value)
            .NotEmpty().WithMessage("Label is required.")
            .MaximumLength(100).WithMessage("Label must not exceed 100 characters.")
            .When(dto => dto.Label is not null);

        RuleFor(dto => dto.Url!.Value)
            .NotEmpty().WithMessage("Url is required.")
            .MaximumLength(1000).WithMessage("Url must not exceed 1000 characters.")
            .Must(url => UrlSchemePolicy.IsAllowed(url, requireHttps))
            .WithMessage(requireHttps
                ? "Url must be a relative path starting with '/' or an HTTPS URL."
                : "Url must be a relative path starting with '/' or an HTTP/HTTPS URL.")
            .When(dto => dto.Url is not null);

        RuleFor(dto => dto.OpenInNewTab!.Value)
            .NotNull().WithMessage("OpenInNewTab group must include Value.")
            .When(dto => dto.OpenInNewTab is not null);

        RuleFor(dto => dto.IsActive!.Value)
            .NotNull().WithMessage("IsActive group must include Value.")
            .When(dto => dto.IsActive is not null);

        RuleFor(dto => dto)
            .Must(dto => dto.Label is not null || dto.Url is not null || dto.OpenInNewTab is not null || dto.IsActive is not null)
            .WithMessage("At least one footer link update must be provided.");
    }

}
