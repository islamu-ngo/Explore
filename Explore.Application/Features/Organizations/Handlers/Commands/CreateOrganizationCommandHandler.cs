using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Organizations.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.Organizations.Handlers.Commands
{
    public class CreateOrganizationCommandHandler : IRequestHandler<CreateOrganizationCommand, BaseCommandResponse<Guid>>
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IOrganizationMemberRepository _organizationMemberRepository;
        private readonly IActorRepository _actorRepository;
        private readonly IUserContext _userContext;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;

        public CreateOrganizationCommandHandler(
            IOrganizationRepository organizationRepository,
            IOrganizationMemberRepository organizationMemberRepository,
            IActorRepository actorRepository,
            IUserContext userContext,
            IMapper mapper,
            ITenantContext tenantContext)
        {
            _organizationRepository = organizationRepository;
            _organizationMemberRepository = organizationMemberRepository;
            _actorRepository = actorRepository;
            _userContext = userContext;
            _mapper = mapper;
            _tenantContext = tenantContext;
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
                TenantId = _tenantContext.TenantId,
                DisplayName = organization.FullName,
                Handle = GenerateHandle(organization.FullName),
                Description = null,
                UserId = null, // Organization actors don't have a UserId
                OrganizationId = organization.Id // Link to the organization
            };

            organizationActor = await _actorRepository.Create(organizationActor);
            Console.WriteLine($"[CREATE ORG] Actor created for organization - ActorId: {organizationActor.Id}");

            // Update Organization to set ActorId
            organization.ActorId = organizationActor.Id;
            await _organizationRepository.Update(organization);
            Console.WriteLine($"[CREATE ORG] Organization updated with ActorId: {organizationActor.Id}");

            // Automatically add the creator as a Creator member
            var organizationMember = new OrganizationMember
            {
                OrganizationId = organization.Id,
                UserId = currentUserId,
                OrganizationRoleId = (int)OrganizationRoleEnum.Creator,
                OrganizationPositionId = null // No position assigned initially
            };

            await _organizationMemberRepository.Create(organizationMember);
            Console.WriteLine($"[CREATE ORG] User {currentUserId} added as Creator of organization {organization.Id}");

            response.Success = true;
            response.Message = "Organization created successfully. You are now the creator and admin of this organization.";
            response.Id = organization.Id;
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
}
