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

            try
            {
                Console.WriteLine($"[USER SYNC] Starting sync for user - ID: {userDto.Id}, Email: {userDto.Email}");
                
                // Check by BOTH ID and EMAIL to handle all cases
                var existingUserById = await _userRepository.GetById(userDto.Id);
                var existingUserByEmail = await _userRepository.GetUserByEmail(userDto.Email);

                Console.WriteLine($"[USER SYNC] Existing user by ID: {(existingUserById != null ? existingUserById.Id.ToString() : "NOT FOUND")}");
                Console.WriteLine($"[USER SYNC] Existing user by email: {(existingUserByEmail != null ? existingUserByEmail.Id.ToString() : "NOT FOUND")}");

                // If user exists by email but different ID, this is a conflict
                if (existingUserByEmail != null && existingUserByEmail.Id != userDto.Id)
                {
                    Console.WriteLine($"[USER SYNC] CONFLICT - Email exists with different ID. DB User ID: {existingUserByEmail.Id}, Keycloak User ID: {userDto.Id}");
                    response.Success = false;
                    response.Message = $"A user with email {userDto.Email} already exists with a different ID. Database ID: {existingUserByEmail.Id}, Keycloak ID: {userDto.Id}";
                    return response;
                }

                // Use whichever exists (prefer by ID)
                var existingUser = existingUserById ?? existingUserByEmail;

                if (existingUser == null)
                {
                    Console.WriteLine($"[USER SYNC] No existing user found - Creating new user");
                    
                    // ===== CREATE NEW USER AND ACTOR WITHOUT FK CIRCULARITY =====
                    // First, get the default tenant (or make configurable)
                    var defaultTenantId = await GetDefaultTenantIdAsync();

                    // Create the User first WITHOUT ActorId to avoid FK constraint
                    var user = new User
                    {
                        Id = userDto.Id, // Use Keycloak ID as User ID
                        Email = userDto.Email,
                        FirstName = userDto.FirstName,
                        LastName = userDto.LastName,
                        ActorId = null, // set later
                        AuthProvider = "keycloak",
                        AuthProviderId = userDto.Id.ToString(),
                        EmailVerified = true, // Keycloak handles email verification
                        DefaultActorId = null
                    };

                    try
                    {
                        user = await _userRepository.Create(user);
                        Console.WriteLine($"[USER SYNC] User created successfully - ID: {user.Id}");
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("ix_users_email"))
                    {
                        Console.WriteLine($"[USER SYNC] Race condition detected - User was created by another thread");
                        
                        // Race condition: Another thread created the user just now
                        // Retry by fetching the newly created user
                        existingUser = await _userRepository.GetUserByEmail(userDto.Email);
                        if (existingUser != null)
                        {
                            Console.WriteLine($"[USER SYNC] Found user created by another thread - ID: {existingUser.Id}");
                            
                            // User was created by another thread, update it instead
                            existingUser.FirstName = userDto.FirstName;
                            existingUser.LastName = userDto.LastName;
                            
                            if (existingUser.ActorId != null)
                            {
                                var actor = await _actorRepository.GetById(existingUser.ActorId.Value);
                                if (actor != null)
                                {
                                    actor.DisplayName = $"{userDto.FirstName} {userDto.LastName}".Trim();
                                    await _actorRepository.Update(actor);
                                }
                            }
                            
                            await _userRepository.Update(existingUser);
                            response.Success = true;
                            response.Message = "User updated successfully (created by another request)";
                            response.Id = existingUser.Id;
                            Console.WriteLine($"[USER SYNC] User updated after race condition - ID: {existingUser.Id}");
                            return response;
                        }
                        
                        Console.WriteLine($"[USER SYNC] ERROR - Could not find user after race condition");
                        throw;
                    }

                    // Now create the Actor referencing the created User
                    var newActor = new Actor
                    {
                        ActorTypeId = (int)ActorTypeEnum.User,
                        TenantId = defaultTenantId,
                        DisplayName = $"{userDto.FirstName} {userDto.LastName}".Trim(),
                        Handle = GenerateHandle(userDto.Username, userDto.Email),
                        Description = null,
                        UserId = user.Id, // Link to the newly created user
                        OrganizationId = null
                    };

                    newActor = await _actorRepository.Create(newActor);
                    Console.WriteLine($"[USER SYNC] Actor created successfully - ID: {newActor.Id}");

                    // Update the User to set ActorId and DefaultActorId
                    user.ActorId = newActor.Id;
                    user.DefaultActorId = newActor.Id;
                    await _userRepository.Update(user);

                    response.Success = true;
                    response.Message = "User and Actor created successfully";
                    response.Id = user.Id;
                    Console.WriteLine($"[USER SYNC] User creation completed - ID: {user.Id}");
                }
                else
                {
                    Console.WriteLine($"[USER SYNC] Existing user found - Updating user ID: {existingUser.Id}");
                    
                    // ===== UPDATE EXISTING USER =====
                    // Only update fields from IDP (Keycloak)
                    // We do NOT overwrite user's custom data
                    existingUser.Email = userDto.Email;
                    existingUser.FirstName = userDto.FirstName;
                    existingUser.LastName = userDto.LastName;
                    
                    // Also update the Actor's display name if it changed
                    if (existingUser.ActorId != null)
                    {
                        var actor = await _actorRepository.GetById(existingUser.ActorId.Value);
                        if (actor != null)
                        {
                            var newDisplayName = $"{userDto.FirstName} {userDto.LastName}".Trim();
                            if (actor.DisplayName != newDisplayName)
                            {
                                Console.WriteLine($"[USER SYNC] Updating actor display name from '{actor.DisplayName}' to '{newDisplayName}'");
                                actor.DisplayName = newDisplayName;
                                await _actorRepository.Update(actor);
                            }
                        }
                    }

                    await _userRepository.Update(existingUser);
                    response.Success = true;
                    response.Message = "User updated successfully";
                    response.Id = existingUser.Id;
                    Console.WriteLine($"[USER SYNC] User update completed - ID: {existingUser.Id}");
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error syncing user: {ex.Message}";
                Console.WriteLine($"[USER SYNC] ERROR - Exception occurred: {ex.GetType().Name}");
                Console.WriteLine($"[USER SYNC] ERROR - Message: {ex.Message}");
                Console.WriteLine($"[USER SYNC] ERROR - StackTrace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[USER SYNC] ERROR - InnerException: {ex.InnerException.GetType().Name}");
                    Console.WriteLine($"[USER SYNC] ERROR - InnerException Message: {ex.InnerException.Message}");
                }
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
