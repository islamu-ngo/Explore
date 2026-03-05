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

        group.FullName = request.GroupDto.FullName;
        group.Description = request.GroupDto.Description;
        group.UpdatedAt = DateTime.UtcNow;
        group.UpdatedBy = currentUserId;

        await _groupRepository.Update(group);

        await _cache.RemoveAsync($"group:detail:{group.Id}", cancellationToken);

        response.Success = true;
        response.Message = "Group updated successfully.";
        response.Id = group.Id;

        return response;
    }
}
