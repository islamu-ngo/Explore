// ABOUTME: Command to update the title and active state of a footer link group.
// ABOUTME: Validates the group belongs to the current tenant before updating.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using FluentValidation;
using MediatR;

namespace Explore.Application.Features.Footer.Requests.Commands;

[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]
public class UpdateFooterLinkGroupCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TenantId { get; set; }
    public Guid GroupId { get; set; }
    public required PatchFooterLinkGroupDto Update { get; set; }
    string? ISecureRequest.ResourceId => TenantId == Guid.Empty ? null : TenantId.ToString("D");
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => TenantId == Guid.Empty
        ? null
        : new Dictionary<string, object>
        {
            ["tenantId"] = TenantId.ToString("D"),
            ["groupId"] = GroupId.ToString("D")
        };

}

public sealed class PatchFooterLinkGroupDto
{
    public PatchFooterLinkGroupTitleDto? Title { get; set; }
    public PatchFooterLinkGroupIsActiveDto? IsActive { get; set; }
}

public sealed class PatchFooterLinkGroupTitleDto
{
    public required string Value { get; set; }
}

public sealed class PatchFooterLinkGroupIsActiveDto
{
    public bool? Value { get; set; }
}

public sealed class PatchFooterLinkGroupDtoValidator : AbstractValidator<PatchFooterLinkGroupDto>
{
    public PatchFooterLinkGroupDtoValidator()
    {
        RuleFor(dto => dto.Title!.Value)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(100).WithMessage("Title must not exceed 100 characters.")
            .When(dto => dto.Title is not null);

        RuleFor(dto => dto.IsActive!.Value)
            .NotNull().WithMessage("IsActive group must include Value.")
            .When(dto => dto.IsActive is not null);

        RuleFor(dto => dto)
            .Must(dto => dto.Title is not null || dto.IsActive is not null)
            .WithMessage("At least one footer link group update must be provided.");
    }
}
