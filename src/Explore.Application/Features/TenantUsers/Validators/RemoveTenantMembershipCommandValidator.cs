// ABOUTME: Validates tenant and user identifiers for tenant-membership removal.
// ABOUTME: Keeps malformed removal requests outside the transaction boundary.

using Explore.Application.Features.TenantUsers.Requests.Commands;
using FluentValidation;

namespace Explore.Application.Features.TenantUsers.Validators;

public sealed class RemoveTenantMembershipCommandValidator : AbstractValidator<RemoveTenantMembershipCommand>
{
    public RemoveTenantMembershipCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.UserId).NotEmpty();
    }
}
