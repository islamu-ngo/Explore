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
    public class UpdateUserExternalLoginCommandHandler : IRequestHandler<UpdateUserExternalLoginCommand, BaseCommandResponse<Guid>>
    {
        private readonly IUserExternalLoginRepository _userExternalLoginRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<UpdateUserExternalLoginDto> _validator;

        public UpdateUserExternalLoginCommandHandler(
            IUserExternalLoginRepository userExternalLoginRepository,
            IMapper mapper,
            IValidator<UpdateUserExternalLoginDto> validator)
        {
            _userExternalLoginRepository = userExternalLoginRepository;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(UpdateUserExternalLoginCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            var validationResult = await _validator.ValidateAsync(request.UserExternalLoginDto);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "User External Login update failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            var existingLogin = await _userExternalLoginRepository.GetById(request.UserExternalLoginDto.Id);
            if (existingLogin == null)
            {
                response.Success = false;
                response.Message = "User External Login not found.";
                return response;
            }

            _mapper.Map(request.UserExternalLoginDto, existingLogin);
            await _userExternalLoginRepository.Update(existingLogin);

            response.Success = true;
            response.Id = existingLogin.Id;
            response.Message = "User External Login updated successfully.";

            return response;
        }
    }
}
