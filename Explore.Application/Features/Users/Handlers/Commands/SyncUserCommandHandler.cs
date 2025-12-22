using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.User;
using Explore.Application.Features.Users.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.Users.Handlers.Commands
{
    public class SyncUserCommandHandler : IRequestHandler<SyncUserCommand, BaseCommandResponse<Guid>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public SyncUserCommandHandler(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(SyncUserCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();
            var userDto = request.UserDto;

            var existingUser = await _userRepository.GetByIdAsync(userDto.Id);

            if (existingUser == null)
            {
                // Create new user
                var user = _mapper.Map<User>(userDto);
                user = await _userRepository.Create(user);
                response.Success = true;
                response.Message = "User created successfully";
                response.Id = user.Id;
            }
            else
            {
                // Update existing user - Only update fields from IDP (Keycloak)
                // We do NOT map the whole object because UserDto has nulls for Bio/City/Country
                // which would overwrite the user's custom data.
                existingUser.Email = userDto.Email;
                existingUser.FirstName = userDto.FirstName;
                existingUser.LastName = userDto.LastName;
                existingUser.Username = userDto.Username;
                
                await _userRepository.Update(existingUser);
                response.Success = true;
                response.Message = "User updated successfully";
                response.Id = existingUser.Id;
            }

            return response;
        }
    }
}
