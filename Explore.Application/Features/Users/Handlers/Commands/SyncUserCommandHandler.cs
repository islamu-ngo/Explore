using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.User;
using Explore.Application.Features.Users.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Explore.Application.Features.Users.Handlers.Commands
{
    public class SyncUserCommandHandler : IRequestHandler<SyncUserCommand, BaseCommandResponse<Guid>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IActorRepository _actorRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public SyncUserCommandHandler(
            IUserRepository userRepository, 
            IActorRepository actorRepository,
            ITenantRepository tenantRepository,
            IMapper mapper,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _actorRepository = actorRepository;
            _tenantRepository = tenantRepository;
            _mapper = mapper;
            _configuration = configuration;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(SyncUserCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();
            var userDto = request.UserDto;

            var existingUser = await _userRepository.GetById(userDto.Id);

            if (existingUser == null)
            {
                // ===== CREATE NEW USER WITH ACTOR =====
                // First, get the default tenant (or make configurable)
                var defaultTenantId = await GetDefaultTenantIdAsync();

                // Create the Actor first (User type actor)
                var actor = new Actor
                {
                    ActorTypeId = (int)ActorTypeEnum.User,
                    TenantId = defaultTenantId,
                    DisplayName = $"{userDto.FirstName} {userDto.LastName}".Trim(),
                    Handle = GenerateHandle(userDto.Username, userDto.Email),
                    Description = null,
                    UserId = userDto.Id, // Link to the user being created
                    OrganizationId = null
                };

                actor = await _actorRepository.Create(actor);

                // Now create the User with the Actor reference
                var user = new User
                {
                    Id = userDto.Id, // Use Keycloak ID as User ID
                    Email = userDto.Email,
                    FirstName = userDto.FirstName,
                    LastName = userDto.LastName,
                    ActorId = actor.Id,
                    AuthProvider = "keycloak",
                    AuthProviderId = userDto.Id.ToString(),
                    EmailVerified = true, // Keycloak handles email verification
                    DefaultActorId = actor.Id
                };

                user = await _userRepository.Create(user);

                response.Success = true;
                response.Message = "User and Actor created successfully";
                response.Id = user.Id;
            }
            else
            {
                // ===== UPDATE EXISTING USER =====
                // Only update fields from IDP (Keycloak)
                // We do NOT overwrite user's custom data
                existingUser.Email = userDto.Email;
                existingUser.FirstName = userDto.FirstName;
                existingUser.LastName = userDto.LastName;
                
                // Also update the Actor's display name if it changed
                var actor = await _actorRepository.GetById(existingUser.ActorId);
                if (actor != null)
                {
                    var newDisplayName = $"{userDto.FirstName} {userDto.LastName}".Trim();
                    if (actor.DisplayName != newDisplayName)
                    {
                        actor.DisplayName = newDisplayName;
                        await _actorRepository.Update(actor);
                    }
                }

                await _userRepository.Update(existingUser);
                response.Success = true;
                response.Message = "User updated successfully";
                response.Id = existingUser.Id;
            }

            return response;
        }

        private async Task<Guid> GetDefaultTenantIdAsync()
        {
            // Try to get from configuration first
            var configuredTenantId = _configuration["DefaultTenantId"];
            if (!string.IsNullOrEmpty(configuredTenantId) && Guid.TryParse(configuredTenantId, out var tenantId))
            {
                return tenantId;
            }

            // Fallback: get the first active tenant
            var tenants = await _tenantRepository.GetAll();
            var defaultTenant = tenants.FirstOrDefault(t => t.IsActive);
            
            if (defaultTenant == null)
            {
                throw new InvalidOperationException("No active tenant found in the system.");
            }

            return defaultTenant.Id;
        }

        private static string GenerateHandle(string? username, string email)
        {
            // Use username if available, otherwise use email prefix
            if (!string.IsNullOrWhiteSpace(username))
            {
                return username.ToLowerInvariant().Replace(" ", "-");
            }

            var emailPrefix = email.Split('@')[0];
            return emailPrefix.ToLowerInvariant().Replace(".", "-").Replace(" ", "-");
        }
    }
}
