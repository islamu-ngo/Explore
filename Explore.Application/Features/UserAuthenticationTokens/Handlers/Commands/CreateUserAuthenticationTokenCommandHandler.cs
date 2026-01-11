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
    public class CreateUserAuthenticationTokenCommandHandler : IRequestHandler<CreateUserAuthenticationTokenCommand, BaseCommandResponse<Guid>>
    {
        private readonly IUserAuthenticationTokenRepository _userAuthenticationTokenRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateUserAuthenticationTokenDto> _validator;

        public CreateUserAuthenticationTokenCommandHandler(
            IUserAuthenticationTokenRepository userAuthenticationTokenRepository,
            IMapper mapper,
            IValidator<CreateUserAuthenticationTokenDto> validator)
        {
            _userAuthenticationTokenRepository = userAuthenticationTokenRepository;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(CreateUserAuthenticationTokenCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            var validationResult = await _validator.ValidateAsync(request.UserAuthenticationTokenDto);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "User Authentication Token creation failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            var token = _mapper.Map<UserAuthenticationToken>(request.UserAuthenticationTokenDto);
            token = await _userAuthenticationTokenRepository.Create(token);

            response.Success = true;
            response.Id = token.Id;
            response.Message = "User Authentication Token created successfully.";

            return response;
        }
    }
}
