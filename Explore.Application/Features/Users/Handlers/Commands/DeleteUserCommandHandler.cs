using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Application.Features.Users.Requests.Commands;
using Explore.Domain;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.Users.Handlers.Commands;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;

    public DeleteUserCommandHandler(IUserRepository userRepository, IMapper mapper, HybridCache cache)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetById(request.UserId);

        if (user == null)
            throw new NotFoundException(nameof(User), request.UserId);

        await _userRepository.Delete(user);
        await _cache.RemoveAsync($"user:detail:{request.UserId}", cancellationToken);

        return Unit.Value;
    }
}
