// ABOUTME: Query handler returning a paginated list of external login records.
// ABOUTME: Maps entities to UserExternalLoginListDto.
using System.Collections.Generic;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.UserExternalLogin;
using Explore.Application.Features.UserExternalLogins.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.UserExternalLogins.Handlers.Queries;

public class GetUserExternalLoginListRequestHandler : IRequestHandler<GetUserExternalLoginListRequest, List<UserExternalLoginListDto>>
{
    private readonly IUserExternalLoginRepository _userExternalLoginRepository;
    private readonly IMapper _mapper;

    public GetUserExternalLoginListRequestHandler(IUserExternalLoginRepository userExternalLoginRepository, IMapper mapper)
    {
        _userExternalLoginRepository = userExternalLoginRepository;
        _mapper = mapper;
    }

    public async Task<List<UserExternalLoginListDto>> Handle(GetUserExternalLoginListRequest request, CancellationToken cancellationToken)
    {
        var logins = await _userExternalLoginRepository.GetUserExternalLoginsWithDetails();
        return _mapper.Map<List<UserExternalLoginListDto>>(logins);
    }
}
