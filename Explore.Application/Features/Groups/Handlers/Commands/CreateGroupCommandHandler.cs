// ABOUTME: Handles Group creation: validates DTO, creates Group, Actor, storage object link, and creator membership.
// ABOUTME: Follows the same pattern as CreateOrganizationCommandHandler with Group-specific entities and roles.

using AutoMapper;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Group.Validators;
using Explore.Application.Features.Groups.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.Groups.Handlers.Commands;

public class CreateGroupCommandHandler : IRequestHandler<CreateGroupCommand, BaseCommandResponse<Guid>>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IUserContext _userContext;
    private readonly IMapper _mapper;
    private readonly ITenantContext _tenantContext;
    private readonly HybridCache _cache;
    private readonly BusinessMetrics _metrics;

    public CreateGroupCommandHandler(
        IGroupRepository groupRepository,
        IGroupMemberRepository groupMemberRepository,
        IActorRepository actorRepository,
        IStorageObjectRepository storageObjectRepository,
        IUserContext userContext,
        IMapper mapper,
        ITenantContext tenantContext,
        HybridCache cache,
        BusinessMetrics metrics)
    {
        _groupRepository = groupRepository;
        _groupMemberRepository = groupMemberRepository;
        _actorRepository = actorRepository;
        _storageObjectRepository = storageObjectRepository;
        _userContext = userContext;
        _mapper = mapper;
        _tenantContext = tenantContext;
        _cache = cache;
        _metrics = metrics;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateGroupCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new CreateGroupDtoValidator();
        var validationResult = await validator.ValidateAsync(request.GroupDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Validation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var currentUserId = _userContext.GetRequiredUserId();

        var group = _mapper.Map<Group>(request.GroupDto);

        group.ApprovalStatusId = (int)ApprovalStatusEnum.Pending;
        group.TenantId = _tenantContext.TenantId;
        group.CreatedAt = DateTime.UtcNow;

        group = await _groupRepository.Create(group);

        var groupActor = new Actor
        {
            ActorTypeId = (int)ActorTypeEnum.Group,
            ActorType = null!,
            TenantId = _tenantContext.TenantId,
            Tenant = null!,
            DisplayName = group.FullName,
            Handle = GenerateHandle(group.FullName),
            Description = null,
            UserId = null,
            OrganizationId = null,
            GroupId = group.Id,
            ProfilePictureId = request.GroupDto.ProfilePictureId
        };

        groupActor = await _actorRepository.Create(groupActor);

        group.ActorId = groupActor.Id;
        await _groupRepository.Update(group);

        if (request.GroupDto.ProfilePictureId.HasValue)
        {
            var storageObject = await _storageObjectRepository.GetById(request.GroupDto.ProfilePictureId.Value);
            if (storageObject != null)
            {
                storageObject.ActorId = groupActor.Id;
                await _storageObjectRepository.Update(storageObject);
            }
        }

        var groupMember = new GroupMember
        {
            GroupId = group.Id,
            Group = null!,
            UserId = currentUserId,
            User = null!,
            RoleId = (int)RoleEnum.GroupAdmin,
            Role = null!,
            TenantId = _tenantContext.TenantId,
            Tenant = null!
        };

        await _groupMemberRepository.Create(groupMember);

        response.Success = true;
        response.Message = "Group created successfully. You are now the creator and admin of this group.";
        response.Id = group.Id;

        _metrics.RecordOrganizationCreated(_tenantContext.TenantId.ToString());

        await _cache.RemoveAsync($"group:detail:{group.Id}", cancellationToken);

        return response;
    }

    private string GenerateHandle(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return $"grp-{Guid.NewGuid().ToString("N").Substring(0, 8)}";

        var handle = name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace("\"", "")
            .Replace(".", "")
            .Replace(",", "");

        handle = System.Text.RegularExpressions.Regex.Replace(handle, @"[^a-z0-9\-]", "");

        if (handle.Length > 20)
            handle = handle.Substring(0, 20);

        return $"{handle}-{Guid.NewGuid().ToString("N").Substring(0, 6)}";
    }
}
