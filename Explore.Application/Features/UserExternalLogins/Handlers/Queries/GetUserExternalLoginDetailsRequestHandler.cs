using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.UserExternalLogin;
using Explore.Application.Features.UserExternalLogins.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.UserExternalLogins.Handlers.Queries;

public class GetUserExternalLoginDetailsRequestHandler : IRequestHandler<GetUserExternalLoginDetailsRequest, UserExternalLoginDto>
{
    private readonly IUserExternalLoginRepository _userExternalLoginRepository;
    private readonly IMapper _mapper;

    public GetUserExternalLoginDetailsRequestHandler(IUserExternalLoginRepository userExternalLoginRepository, IMapper mapper)
    {
        _userExternalLoginRepository = userExternalLoginRepository;
        _mapper = mapper;
    }

    public async Task<UserExternalLoginDto> Handle(GetUserExternalLoginDetailsRequest request, CancellationToken cancellationToken)
    {
        var login = await _userExternalLoginRepository.GetUserExternalLoginWithDetails(request.Id);
        if (login == null)
        {
            return null;
        }

        return _mapper.Map<UserExternalLoginDto>(login);
    }
}
