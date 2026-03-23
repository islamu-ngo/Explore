// ABOUTME: Handler for creating a new external login link with validation.
// ABOUTME: Validates input, maps DTO, links user to external identity provider.
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.UserExternalLogin;
using Explore.Application.DTOs.UserExternalLogin.Validators;
using Explore.Application.Features.UserExternalLogins.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using FluentValidation;
using MediatR;

namespace Explore.Application.Features.UserExternalLogins.Handlers.Commands;

public class CreateUserExternalLoginCommandHandler : IRequestHandler<CreateUserExternalLoginCommand, BaseCommandResponse<Guid>>
{
    private readonly IUserExternalLoginRepository _userExternalLoginRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;

    public CreateUserExternalLoginCommandHandler(
        IUserExternalLoginRepository userExternalLoginRepository,
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        ITenantContext tenantContext,
        IMapper mapper)
    {
        _userExternalLoginRepository = userExternalLoginRepository;
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateUserExternalLoginCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new CreateUserExternalLoginDtoValidator(_userRepository, _tenantRepository);
        var validationResult = await validator.ValidateAsync(request.UserExternalLoginDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "User External Login creation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var login = _mapper.Map<UserExternalLogin>(request.UserExternalLoginDto);

        // Set TenantId from the request context
        login.TenantId = _tenantContext.TenantId;

        login = await _userExternalLoginRepository.Create(login);

        response.Success = true;
        response.Id = login.Id;
        response.Message = "User External Login created successfully.";

        return response;
    }
}
