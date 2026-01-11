using MediatR;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.UserAuthenticationToken;
using Explore.Application.Features.UserAuthenticationTokens.Requests.Queries;

namespace Explore.Application.Features.UserAuthenticationTokens.Handlers.Queries
{
    public class GetUserAuthenticationTokenDetailsRequestHandler : IRequestHandler<GetUserAuthenticationTokenDetailsRequest, UserAuthenticationTokenDto>
    {
        private readonly IUserAuthenticationTokenRepository _userAuthenticationTokenRepository;
        private readonly IMapper _mapper;

        public GetUserAuthenticationTokenDetailsRequestHandler(IUserAuthenticationTokenRepository userAuthenticationTokenRepository, IMapper mapper)
        {
            _userAuthenticationTokenRepository = userAuthenticationTokenRepository;
            _mapper = mapper;
        }

        public async Task<UserAuthenticationTokenDto> Handle(GetUserAuthenticationTokenDetailsRequest request, CancellationToken cancellationToken)
        {
            var token = await _userAuthenticationTokenRepository.GetUserAuthenticationTokenWithDetails(request.Id);
            if (token == null)
            {
                return null;
            }

            return _mapper.Map<UserAuthenticationTokenDto>(token);
        }
    }
}
