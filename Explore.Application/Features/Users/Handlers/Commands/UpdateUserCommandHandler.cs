using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Users.Requests.Commands;
using Explore.Application.Responses;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Explore.Application.Features.Users.Handlers.Commands
{
    public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, BaseCommandResponse<Guid>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IActorRepository _actorRepository;
        private readonly IMapper _mapper;

        public UpdateUserCommandHandler(IUserRepository userRepository, IActorRepository actorRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _actorRepository = actorRepository;
            _mapper = mapper;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();
            var user = await _userRepository.GetById(request.UpdateUserDto.Id);

            if (user == null)
            {
                response.Success = false;
                response.Message = "User not found";
                return response;
            }

            _mapper.Map(request.UpdateUserDto, user);
            await _userRepository.Update(user);

            // Handle profile picture update
            if (request.UpdateUserDto.ProfilePictureId.HasValue && user.ActorId.HasValue)
            {
                var actor = await _actorRepository.GetById(user.ActorId.Value);
                if (actor != null)
                {
                    actor.ProfilePictureId = request.UpdateUserDto.ProfilePictureId.Value;
                    await _actorRepository.Update(actor);
                }
            }

            response.Success = true;
            response.Message = "User updated successfully";
            response.Id = user.Id;

            return response;
        }
    }
}
