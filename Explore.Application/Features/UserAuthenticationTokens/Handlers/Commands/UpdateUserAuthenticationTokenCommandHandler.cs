using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.UserAuthenticationToken;
using Explore.Application.DTOs.UserAuthenticationToken.Validators;
using Explore.Application.Features.UserAuthenticationTokens.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using FluentValidation;
using MediatR;

namespace Explore.Application.Features.UserAuthenticationTokens.Handlers.Commands;

public class UpdateUserAuthenticationTokenCommandHandler : IRequestHandler<UpdateUserAuthenticationTokenCommand, BaseCommandResponse<Guid>>
{
    private readonly IUserAuthenticationTokenRepository _userAuthenticationTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IMapper _mapper;

    public UpdateUserAuthenticationTokenCommandHandler(
        IUserAuthenticationTokenRepository userAuthenticationTokenRepository,
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        IMapper mapper)
    {
        _userAuthenticationTokenRepository = userAuthenticationTokenRepository;
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateUserAuthenticationTokenCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateUserAuthenticationTokenDtoValidator(_userRepository, _tenantRepository);
        var validationResult = await validator.ValidateAsync(request.UserAuthenticationTokenDto, cancellationToken);

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
