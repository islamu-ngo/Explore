using MediatR;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.IndexedDid.Validators;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Application.Features.IndexedDids.Requests.Commands;

namespace Explore.Application.Features.IndexedDids.Handlers.Commands
{
    public class UpdateIndexedDidCommandHandler : IRequestHandler<UpdateIndexedDidCommand, BaseCommandResponse<string>>
    {
        private readonly IIndexedDidRepository _indexedDidRepository;
        private readonly IMapper _mapper;

        public UpdateIndexedDidCommandHandler(
            IIndexedDidRepository indexedDidRepository,
            IMapper mapper)
        {
            _indexedDidRepository = indexedDidRepository;
            _mapper = mapper;
        }

        public async Task<BaseCommandResponse<string>> Handle(UpdateIndexedDidCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<string>();

            // Create validator instance with dependencies
            var validator = new UpdateIndexedDidDtoValidator(_indexedDidRepository);
            var validationResult = await validator.ValidateAsync(request.IndexedDidDto);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "IndexedDid update failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            var indexedDid = _mapper.Map<IndexedDid>(request.IndexedDidDto);

            await _indexedDidRepository.Update(indexedDid);

            response.Success = true;
            response.Id = indexedDid.Did;
            response.Message = "IndexedDid updated successfully.";

            return response;
        }
    }
}
