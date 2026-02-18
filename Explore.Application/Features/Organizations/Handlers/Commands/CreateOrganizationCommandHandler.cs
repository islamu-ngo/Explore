using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Organizations.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.Organizations.Handlers.Commands;

public class CreateOrganizationCommandHandler : IRequestHandler<CreateOrganizationCommand, BaseCommandResponse<Guid>>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrganizationMemberRepository _organizationMemberRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IUserContext _userContext;
    private readonly IMapper _mapper;
    private readonly ITenantContext _tenantContext;
    private readonly HybridCache _cache;
    private readonly BusinessMetrics _metrics;

    public CreateOrganizationCommandHandler(
        IOrganizationRepository organizationRepository,
        IOrganizationMemberRepository organizationMemberRepository,
        IActorRepository actorRepository,
        IStorageObjectRepository storageObjectRepository,
        IUserContext userContext,
        IMapper mapper,
        ITenantContext tenantContext,
        HybridCache cache,
        BusinessMetrics metrics)
    {
        _organizationRepository = organizationRepository;
        _organizationMemberRepository = organizationMemberRepository;
        _actorRepository = actorRepository;
        _storageObjectRepository = storageObjectRepository;
        _userContext = userContext;
        _mapper = mapper;
        _tenantContext = tenantContext;
        _cache = cache;
        _metrics = metrics;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        // Get the current authenticated user
        var currentUserId = _userContext.GetRequiredUserId();

        var organization = _mapper.Map<Organization>(request.OrganizationDto);

        // Set required fields
        organization.ApprovalStatusId = (int)ApprovalStatusEnum.Pending;
        organization.TenantId = _tenantContext.TenantId;
        organization.CreatedAt = DateTime.UtcNow;

        // Create the Organization first (without ActorId)
        organization = await _organizationRepository.Create(organization);

        Console.WriteLine($"[CREATE ORG] Organization created - ID: {organization.Id}, Name: {organization.FullName}");

        // ===== CREATE ACTOR FOR ORGANIZATION =====
        // Just like User has an Actor, Organization also needs an Actor
        // This enables the organization to be the "poster" of events
        var organizationActor = new Actor
        {
            ActorTypeId = (int)ActorTypeEnum.Organization,
            ActorType = null!,
            TenantId = _tenantContext.TenantId,
            Tenant = null!,
            DisplayName = organization.FullName,
            Handle = GenerateHandle(organization.FullName),
            Description = null,
            UserId = null, // Organization actors don't have a UserId
            OrganizationId = organization.Id, // Link to the organization
            ProfilePictureId = request.OrganizationDto.ProfilePictureId // Set profile picture if provided
        };

        organizationActor = await _actorRepository.Create(organizationActor);
        Console.WriteLine($"[CREATE ORG] Actor created for organization - ActorId: {organizationActor.Id}");

        // Update Organization to set ActorId
        organization.ActorId = organizationActor.Id;
        await _organizationRepository.Update(organization);
        Console.WriteLine($"[CREATE ORG] Organization updated with ActorId: {organizationActor.Id}");

        // ===== UPDATE STORAGE OBJECT OWNERSHIP =====
        // If a profile picture was uploaded, update its ActorId to link it to this organization's actor
        if (request.OrganizationDto.ProfilePictureId.HasValue)
        {
            var storageObject = await _storageObjectRepository.GetById(request.OrganizationDto.ProfilePictureId.Value);
            if (storageObject != null)
            {
                storageObject.ActorId = organizationActor.Id;
                await _storageObjectRepository.Update(storageObject);
                Console.WriteLine($"[CREATE ORG] StorageObject {storageObject.Id} ActorId updated to {organizationActor.Id}");
            }
        }

        // Automatically add the creator as a Creator member
        var organizationMember = new OrganizationMember
        {
            OrganizationId = organization.Id,
            Organization = null!,
            UserId = currentUserId,
            User = null!,
            RoleId = (int)RoleEnum.OrgCreator,
            Role = null!,
            OrganizationPositionId = null, // No position assigned initially
            TenantId = _tenantContext.TenantId, // Required for multi-tenant isolation
            Tenant = null!
        };

        await _organizationMemberRepository.Create(organizationMember);
        Console.WriteLine($"[CREATE ORG] User {currentUserId} added as Creator of organization {organization.Id}");

        response.Success = true;
        response.Message = "Organization created successfully. You are now the creator and admin of this organization.";
        response.Id = organization.Id;

        _metrics.RecordOrganizationCreated(_tenantContext.TenantId.ToString());

        await _cache.RemoveAsync($"organization:detail:{organization.Id}", cancellationToken);

        return response;
    }

    /// <summary>
    /// Generate a URL-friendly handle from the organization name
    /// </summary>
    private string GenerateHandle(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return $"org-{Guid.NewGuid().ToString("N").Substring(0, 8)}";

        // Convert to lowercase, replace spaces with hyphens, remove special characters
        var handle = name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace("\"", "")
            .Replace(".", "")
            .Replace(",", "");

        // Remove any non-alphanumeric characters except hyphens
        handle = System.Text.RegularExpressions.Regex.Replace(handle, @"[^a-z0-9\-]", "");

        // Limit length and add unique suffix to avoid collisions
        if (handle.Length > 20)
            handle = handle.Substring(0, 20);

        return $"{handle}-{Guid.NewGuid().ToString("N").Substring(0, 6)}";
    }
}
