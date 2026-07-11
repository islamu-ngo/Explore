// ABOUTME: Handler for updating an existing AT Protocol record with validation.
// ABOUTME: Validates input, fetches entity, applies field updates.
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.AtprotoRecord.Validators;
using Explore.Application.Features.AtprotoRecords.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.AtprotoRecords.Handlers.Commands;

public class UpdateAtprotoRecordCommandHandler : IRequestHandler<UpdateAtprotoRecordCommand, BaseCommandResponse<Guid>>
{
    private readonly IAtprotoRecordRepository _atprotoRecordRepository;
    private readonly IMapper _mapper;

    public UpdateAtprotoRecordCommandHandler(
        IAtprotoRecordRepository atprotoRecordRepository,
        IMapper mapper)
    {
        _atprotoRecordRepository = atprotoRecordRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateAtprotoRecordCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        // Create validator instance with dependencies
        var validator = new UpdateAtprotoRecordDtoValidator(_atprotoRecordRepository);
        var validationResult = await validator.ValidateAsync(request.AtprotoRecordDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "AtprotoRecord update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var atprotoRecord = _mapper.Map<AtprotoRecord>(request.AtprotoRecordDto);

        await _atprotoRecordRepository.Update(atprotoRecord);

        response.Success = true;
        response.Id = atprotoRecord.Id;
        response.Message = "AtprotoRecord updated successfully.";

        return response;
    }
}
