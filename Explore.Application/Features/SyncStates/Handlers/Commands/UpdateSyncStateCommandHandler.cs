using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.SyncState.Validators;
using Explore.Application.Features.SyncStates.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.SyncStates.Handlers.Commands;

public class UpdateSyncStateCommandHandler : IRequestHandler<UpdateSyncStateCommand, BaseCommandResponse<int>>
{
    private readonly ISyncStateRepository _syncStateRepository;
    private readonly IMapper _mapper;

    public UpdateSyncStateCommandHandler(
        ISyncStateRepository syncStateRepository,
        IMapper mapper)
    {
        _syncStateRepository = syncStateRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<int>> Handle(UpdateSyncStateCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<int>();

        // Create validator instance with dependencies
        var validator = new UpdateSyncStateDtoValidator(_syncStateRepository);
        var validationResult = await validator.ValidateAsync(request.SyncStateDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "SyncState update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var syncState = _mapper.Map<SyncState>(request.SyncStateDto);
        syncState.UpdatedAt = DateTime.UtcNow;

        await _syncStateRepository.Update(syncState);

        response.Success = true;
        response.Id = syncState.Id;
        response.Message = "SyncState updated successfully.";

        return response;
    }
}
