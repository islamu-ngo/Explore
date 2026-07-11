// ABOUTME: Handler for creating a new user authentication token with validation.
// ABOUTME: Validates input, maps DTO, persists via repository.
using AutoMapper;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.UserAuthenticationToken;
using Explore.Application.DTOs.UserAuthenticationToken.Validators;
using Explore.Application.Exceptions;
using Explore.Application.Features.UserAuthenticationTokens.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using FluentValidation;
using MediatR;

namespace Explore.Application.Features.UserAuthenticationTokens.Handlers.Commands;

public class CreateUserAuthenticationTokenCommandHandler : IRequestHandler<CreateUserAuthenticationTokenCommand, BaseCommandResponse<Guid>>
{
    private readonly IUserAuthenticationTokenRepository _userAuthenticationTokenRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public CreateUserAuthenticationTokenCommandHandler(
        IUserAuthenticationTokenRepository userAuthenticationTokenRepository,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _userAuthenticationTokenRepository = userAuthenticationTokenRepository;
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateUserAuthenticationTokenCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var currentUserId = _currentUserService.UserId
            ?? throw new AuthorizationException(ResourceKinds.User, AuthorizationActions.Users.Update);

        var validator = new CreateUserAuthenticationTokenDtoValidator();
        var validationResult = await validator.ValidateAsync(request.UserAuthenticationTokenDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "User Authentication Token creation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var token = _mapper.Map<UserAuthenticationToken>(request.UserAuthenticationTokenDto);

        token.UserId = currentUserId;
        token.TenantId = _tenantContext.TenantId;

        token = await _userAuthenticationTokenRepository.Create(token);

        response.Success = true;
        response.Id = token.Id;
        response.Message = "User Authentication Token created successfully.";

        return response;
    }
}
