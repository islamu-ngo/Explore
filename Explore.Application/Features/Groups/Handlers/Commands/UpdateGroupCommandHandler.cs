// ABOUTME: Handles Group update: verifies user permission, validates DTO, and updates Group fields.
// ABOUTME: Requires GroupManage permission via IGroupMemberRepository.HasPermissionInGroup.

using AutoMapper;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Group.Validators;
using Explore.Application.Exceptions;
using Explore.Application.Features.Groups.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Constants;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.Groups.Handlers.Commands;

public class UpdateGroupCommandHandler : IRequestHandler<UpdateGroupCommand, BaseCommandResponse<Guid>>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly IUserContext _userContext;
    private readonly IMapper _mapper;
    private readonly ITenantContext _tenantContext;
    private readonly HybridCache _cache;

    public UpdateGroupCommandHandler(
        IGroupRepository groupRepository,
        IGroupMemberRepository groupMemberRepository,
        IUserContext userContext,
        IMapper mapper,
        ITenantContext tenantContext,
        HybridCache cache)
    {
        _groupRepository = groupRepository;
        _groupMemberRepository = groupMemberRepository;
        _userContext = userContext;
        _mapper = mapper;
        _tenantContext = tenantContext;
        _cache = cache;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateGroupCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var currentUserId = _userContext.GetRequiredUserId();

        var hasPermission = await _groupMemberRepository.HasPermissionInGroup(
            request.Id, currentUserId, PermissionCodes.GroupManage);
        if (!hasPermission)
        {
            response.Success = false;
            response.Message = "You do not have permission to manage this group.";
            return response;
        }

        var group = await _groupRepository.GetById(request.Id);
        if (group == null)
        {
            throw new NotFoundException(nameof(Group), request.Id);
        }

        var validator = new UpdateGroupDtoValidator();
        var validationResult = await validator.ValidateAsync(request.GroupDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Validation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        return await _groupRepository.ExecuteWithHierarchyMutationLock(
            _tenantContext.TenantId,
            async lockedCancellationToken =>
            {
                var hierarchyErrors = await ValidateHierarchy(
                    request.Id,
                    request.GroupDto.ParentOrganizationId,
                    request.GroupDto.ParentGroupId,
                    lockedCancellationToken);
                if (hierarchyErrors.Count > 0)
                {
                    response.Success = false;
                    response.Message = "Validation failed.";
                    response.Errors = hierarchyErrors;
                    return response;
                }

                group.FullName = request.GroupDto.FullName;
                group.Description = request.GroupDto.Description;
                group.ParentOrganizationId = request.GroupDto.ParentOrganizationId;
                group.ParentGroupId = request.GroupDto.ParentGroupId;
                group.UpdatedAt = DateTime.UtcNow;
                group.UpdatedBy = currentUserId;

                await _groupRepository.Update(group);

                await _cache.RemoveAsync($"group:detail:{group.Id}", lockedCancellationToken);

                response.Success = true;
                response.Message = "Group updated successfully.";
                response.Id = group.Id;

                return response;
            },
            cancellationToken);
    }

    private async Task<List<string>> ValidateHierarchy(Guid groupId, Guid? parentOrganizationId, Guid? parentGroupId, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var tenantId = _tenantContext.TenantId;

        if (parentOrganizationId.HasValue)
        {
            var exists = await _groupRepository.OrganizationExistsInTenant(parentOrganizationId.Value, tenantId, cancellationToken);
            if (!exists)
            {
                errors.Add("Parent organization does not exist in the current tenant.");
            }
        }

        if (parentGroupId.HasValue)
        {
            if (parentGroupId.Value == groupId)
            {
                errors.Add("A group cannot be its own parent.");
                return errors;
            }

            var exists = await _groupRepository.GroupExistsInTenant(parentGroupId.Value, tenantId, cancellationToken);
            if (!exists)
            {
                errors.Add("Parent group does not exist in the current tenant.");
            }
            else
            {
                if (await _groupRepository.WouldCreateHierarchyCycle(groupId, parentGroupId.Value, tenantId, cancellationToken))
                {
                    errors.Add("Parent group would create a hierarchy cycle.");
                }

                if (await _groupRepository.WouldExceedHierarchyDepthForMove(groupId, parentGroupId, tenantId, GroupHierarchyRules.MaxDepth, cancellationToken))
                {
                    errors.Add("Parent group hierarchy exceeds the maximum supported depth.");
                }
            }
        }

        return errors;
    }
}
