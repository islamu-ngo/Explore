// ABOUTME: Handler for updating a user external login record with validation.
// ABOUTME: Validates input, fetches entity, applies updates.
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.UserExternalLogin;
using Explore.Application.DTOs.UserExternalLogin.Validators;
using Explore.Application.Features.UserExternalLogins.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using FluentValidation;
using MediatR;

namespace Explore.Application.Features.UserExternalLogins.Handlers.Commands;

public class UpdateUserExternalLoginCommandHandler : IRequestHandler<UpdateUserExternalLoginCommand, BaseCommandResponse<Guid>>
{
    private readonly IUserExternalLoginRepository _userExternalLoginRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IMapper _mapper;

    public UpdateUserExternalLoginCommandHandler(
        IUserExternalLoginRepository userExternalLoginRepository,
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        IMapper mapper)
    {
        _userExternalLoginRepository = userExternalLoginRepository;
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateUserExternalLoginCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateUserExternalLoginDtoValidator(_userRepository, _tenantRepository);
        var validationResult = await validator.ValidateAsync(request.UserExternalLoginDto, cancellationToken);

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
