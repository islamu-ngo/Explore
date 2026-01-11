using MediatR;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.UserExternalLogin;
using Explore.Application.Features.UserExternalLogins.Requests.Commands;
using Explore.Application.DTOs.UserExternalLogin.Validators;
using Explore.Application.Responses;
using Explore.Domain;
using FluentValidation;

namespace Explore.Application.Features.UserExternalLogins.Handlers.Commands
{
    public class CreateUserExternalLoginCommandHandler : IRequestHandler<CreateUserExternalLoginCommand, BaseCommandResponse<Guid>>
    {
        private readonly IUserExternalLoginRepository _userExternalLoginRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateUserExternalLoginDto> _validator;

        public CreateUserExternalLoginCommandHandler(
            IUserExternalLoginRepository userExternalLoginRepository,
            IMapper mapper,
            IValidator<CreateUserExternalLoginDto> validator)
        {
            _userExternalLoginRepository = userExternalLoginRepository;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(CreateUserExternalLoginCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            var validationResult = await _validator.ValidateAsync(request.UserExternalLoginDto);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "User External Login creation failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            var login = _mapper.Map<UserExternalLogin>(request.UserExternalLoginDto);
            login = await _userExternalLoginRepository.Create(login);

            response.Success = true;
            response.Id = login.Id;
            response.Message = "User External Login created successfully.";

            return response;
        }
    }
}
