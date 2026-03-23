// ABOUTME: Query handler returning a paginated list of user authentication tokens.
// ABOUTME: Maps entities to UserAuthenticationTokenListDto.
using System.Collections.Generic;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.UserAuthenticationToken;
using Explore.Application.Features.UserAuthenticationTokens.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.UserAuthenticationTokens.Handlers.Queries;

public class GetUserAuthenticationTokenListRequestHandler : IRequestHandler<GetUserAuthenticationTokenListRequest, List<UserAuthenticationTokenListDto>>
{
    private readonly IUserAuthenticationTokenRepository _userAuthenticationTokenRepository;
    private readonly IMapper _mapper;

    public GetUserAuthenticationTokenListRequestHandler(IUserAuthenticationTokenRepository userAuthenticationTokenRepository, IMapper mapper)
    {
        _userAuthenticationTokenRepository = userAuthenticationTokenRepository;
        _mapper = mapper;
    }

    public async Task<List<UserAuthenticationTokenListDto>> Handle(GetUserAuthenticationTokenListRequest request, CancellationToken cancellationToken)
    {
        var tokens = await _userAuthenticationTokenRepository.GetUserAuthenticationTokensWithDetails();
        return _mapper.Map<List<UserAuthenticationTokenListDto>>(tokens);
    }
}
