// ABOUTME: Handler for updating a user authentication token with validation.
// ABOUTME: Validates input, fetches entity, applies updates.
using AutoMapper;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.UserAuthenticationToken;
using Explore.Application.DTOs.UserAuthenticationToken.Validators;
using Explore.Application.Exceptions;
using Explore.Application.Features.UserAuthenticationTokens.Requests.Commands;
using Explore.Application.Responses;
using FluentValidation;
using MediatR;

namespace Explore.Application.Features.UserAuthenticationTokens.Handlers.Commands;

public class UpdateUserAuthenticationTokenCommandHandler : IRequestHandler<UpdateUserAuthenticationTokenCommand, BaseCommandResponse<Guid>>
{
    private readonly IUserAuthenticationTokenRepository _userAuthenticationTokenRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public UpdateUserAuthenticationTokenCommandHandler(
        IUserAuthenticationTokenRepository userAuthenticationTokenRepository,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _userAuthenticationTokenRepository = userAuthenticationTokenRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateUserAuthenticationTokenCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var currentUserId = _currentUserService.UserId
            ?? throw new AuthorizationException(ResourceKinds.User, AuthorizationActions.Users.Update);

        var validator = new UpdateUserAuthenticationTokenDtoValidator();
        var validationResult = await validator.ValidateAsync(request.UserAuthenticationTokenDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "User Authentication Token update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var existingToken = await _userAuthenticationTokenRepository.GetByIdForUser(
            request.UserAuthenticationTokenDto.Id,
            currentUserId,
            cancellationToken);
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
