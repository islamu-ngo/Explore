using MediatR;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.UserAuthenticationToken;
using Explore.Application.Features.UserAuthenticationTokens.Requests.Commands;
using Explore.Application.DTOs.UserAuthenticationToken.Validators;
using Explore.Application.Responses;
using Explore.Domain;
using FluentValidation;

namespace Explore.Application.Features.UserAuthenticationTokens.Handlers.Commands
{
    public class UpdateUserAuthenticationTokenCommandHandler : IRequestHandler<UpdateUserAuthenticationTokenCommand, BaseCommandResponse<Guid>>
    {
        private readonly IUserAuthenticationTokenRepository _userAuthenticationTokenRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<UpdateUserAuthenticationTokenDto> _validator;

        public UpdateUserAuthenticationTokenCommandHandler(
            IUserAuthenticationTokenRepository userAuthenticationTokenRepository,
            IMapper mapper,
            IValidator<UpdateUserAuthenticationTokenDto> validator)
        {
            _userAuthenticationTokenRepository = userAuthenticationTokenRepository;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(UpdateUserAuthenticationTokenCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            var validationResult = await _validator.ValidateAsync(request.UserAuthenticationTokenDto);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "User Authentication Token update failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            var existingToken = await _userAuthenticationTokenRepository.GetById(request.UserAuthenticationTokenDto.Id);
            if (existingToken == null)
            {
                response.Success = false;
                response.Message = "User Authentication Token not found.";
                return response;
            }

            _mapper.Map(request.UserAuthenticationTokenDto, existingToken);
            await _userAuthenticationTokenRepository.Update(existingToken);

            response.Success = true;
            response.Id = existingToken.Id;
            response.Message = "User Authentication Token updated successfully.";

            return response;
        }
    }
}
