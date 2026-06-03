// ABOUTME: Handles deletion of a user account including hard removal of PII, auth tokens, and linked actor PII.
// ABOUTME: Uses IUnitOfWork to ensure all deletions are atomic — a partial delete is worse than no delete.

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
    private readonly IGenericRepository<UserPii, Guid> _userPiiRepository;
    private readonly IUserAuthenticationTokenRepository _userAuthenticationTokenRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IGenericRepository<ActorPii, Guid> _actorPiiRepository;
    private readonly HybridCache _cache;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteUserCommandHandler(
        IUserRepository userRepository,
        IGenericRepository<UserPii, Guid> userPiiRepository,
        IUserAuthenticationTokenRepository userAuthenticationTokenRepository,
        IActorRepository actorRepository,
        IGenericRepository<ActorPii, Guid> actorPiiRepository,
        HybridCache cache,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _userPiiRepository = userPiiRepository;
        _userAuthenticationTokenRepository = userAuthenticationTokenRepository;
        _actorRepository = actorRepository;
        _actorPiiRepository = actorPiiRepository;
        _cache = cache;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetById(request.UserId);

        if (user == null)
            throw new NotFoundException(nameof(User), request.UserId);

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            // Hard delete user-identifying PII while preserving skeletal user record for analytics.
            var userPii = await _userPiiRepository.GetById(request.UserId);
            if (userPii != null)
            {
                await _userPiiRepository.HardDelete(userPii);
            }

            // Revoke all active authentication tokens.
            var tokens = await _userAuthenticationTokenRepository.GetByUser(request.UserId);
            foreach (var token in tokens)
            {
                await _userAuthenticationTokenRepository.HardDelete(token);
            }

            // Anonymize linked actor identity PII so events remain visible without revealing the deleted user's identity.
            var actor = await _actorRepository.GetActorByUserId(request.UserId);
            if (actor != null)
            {
                var actorPii = await _actorPiiRepository.GetById(actor.Id);
                if (actorPii != null)
                {
                    actorPii.DisplayName = $"DeletedUser{Guid.NewGuid():N}";
                    actorPii.Did = null;
                    actorPii.Handle = null;
                    actorPii.ProfilePictureUri = null;
                    await _actorPiiRepository.Update(actorPii);
                }
            }

            await _userRepository.Delete(user);
        }, cancellationToken);

        await _cache.RemoveAsync($"user:detail:{request.UserId}", cancellationToken);

        return Unit.Value;
    }
}
