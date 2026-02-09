using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.SyncState.Validators;
using Explore.Application.Features.SyncStates.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.SyncStates.Handlers.Commands;

public class CreateSyncStateCommandHandler : IRequestHandler<CreateSyncStateCommand, BaseCommandResponse<int>>
{
    private readonly ISyncStateRepository _syncStateRepository;
    private readonly IMapper _mapper;

    public CreateSyncStateCommandHandler(
        ISyncStateRepository syncStateRepository,
        IMapper mapper)
    {
        _syncStateRepository = syncStateRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<int>> Handle(CreateSyncStateCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<int>();

        // Create validator instance with dependencies
        var validator = new CreateSyncStateDtoValidator();
        var validationResult = await validator.ValidateAsync(request.SyncStateDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "SyncState creation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var syncState = _mapper.Map<SyncState>(request.SyncStateDto);
        syncState.UpdatedAt = DateTime.UtcNow;

        syncState = await _syncStateRepository.Create(syncState);

        response.Success = true;
        response.Id = syncState.Id;
        response.Message = "SyncState created successfully.";

        return response;
    }
}
