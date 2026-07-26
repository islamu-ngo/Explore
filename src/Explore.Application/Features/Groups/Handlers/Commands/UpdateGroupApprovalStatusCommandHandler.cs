// ABOUTME: Handles admin approval-state changes for tenant-scoped Group records.
// ABOUTME: Validates approval lookup rows and updates only approval audit fields, not ordinary group metadata.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Group.Validators;
using Explore.Application.Exceptions;
using Explore.Application.Features.Groups.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.Groups.Handlers.Commands;

public class UpdateGroupApprovalStatusCommandHandler(
    IGroupTenantRepository groupTenantRepository,
    IApprovalStatusRepository approvalStatusRepository,
    ICurrentUserService currentUserService,
    ITenantContext tenantContext,
    HybridCache cache)
    : IRequestHandler<UpdateGroupApprovalStatusCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        UpdateGroupApprovalStatusCommand request,
        CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        var validator = new UpdateGroupApprovalStatusDtoValidator(approvalStatusRepository);
        var validationResult = await validator.ValidateAsync(request.GroupApprovalStatusDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Validation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var participation = await groupTenantRepository.GetByGroupAndTenant(
            request.Id,
            tenantContext.TenantId,
            cancellationToken);
        if (participation == null)
        {
            throw new NotFoundException(nameof(Group), request.Id);
        }

        var currentUserId = currentUserService.UserId;
        participation.ApprovalStatusId = request.GroupApprovalStatusDto.ApprovalStatusId;
        participation.UpdatedAt = DateTime.UtcNow;
        participation.UpdatedBy = currentUserId;

        await groupTenantRepository.Update(participation);
        await cache.RemoveAsync($"group:detail:{participation.GroupId}", cancellationToken);

        response.Success = true;
        response.Message = "Group approval status updated successfully.";
        response.Id = participation.GroupId;

        return response;
    }
}
