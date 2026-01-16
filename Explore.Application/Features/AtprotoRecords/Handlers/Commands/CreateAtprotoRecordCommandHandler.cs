using MediatR;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.AtprotoRecords.Requests.Commands;
using Explore.Application.DTOs.AtprotoRecord.Validators;
using Explore.Application.Responses;
using Explore.Domain;

namespace Explore.Application.Features.AtprotoRecords.Handlers.Commands
{
    public class CreateAtprotoRecordCommandHandler : IRequestHandler<CreateAtprotoRecordCommand, BaseCommandResponse<Guid>>
    {
        private readonly IAtprotoRecordRepository _atprotoRecordRepository;
        private readonly IMapper _mapper;

        public CreateAtprotoRecordCommandHandler(
            IAtprotoRecordRepository atprotoRecordRepository,
            IMapper mapper)
        {
            _atprotoRecordRepository = atprotoRecordRepository;
            _mapper = mapper;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(CreateAtprotoRecordCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            // Create validator instance with dependencies
            var validator = new CreateAtprotoRecordDtoValidator();
            var validationResult = await validator.ValidateAsync(request.AtprotoRecordDto);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "AtprotoRecord creation failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            var atprotoRecord = _mapper.Map<AtprotoRecord>(request.AtprotoRecordDto);

            atprotoRecord = await _atprotoRecordRepository.Create(atprotoRecord);

            response.Success = true;
            response.Id = atprotoRecord.Id;
            response.Message = "AtprotoRecord created successfully.";

            return response;
        }
    }
}
