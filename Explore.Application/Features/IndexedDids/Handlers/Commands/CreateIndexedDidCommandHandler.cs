using MediatR;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.IndexedDid.Validators;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Application.Features.IndexedDids.Requests.Commands;

namespace Explore.Application.Features.IndexedDids.Handlers.Commands
{
    public class CreateIndexedDidCommandHandler : IRequestHandler<CreateIndexedDidCommand, BaseCommandResponse<string>>
    {
        private readonly IIndexedDidRepository _indexedDidRepository;
        private readonly IMapper _mapper;

        public CreateIndexedDidCommandHandler(
            IIndexedDidRepository indexedDidRepository,
            IMapper mapper)
        {
            _indexedDidRepository = indexedDidRepository;
            _mapper = mapper;
        }

        public async Task<BaseCommandResponse<string>> Handle(CreateIndexedDidCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<string>();

            // Create validator instance with dependencies
            var validator = new CreateIndexedDidDtoValidator();
            var validationResult = await validator.ValidateAsync(request.IndexedDidDto);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "IndexedDid creation failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            var indexedDid = _mapper.Map<IndexedDid>(request.IndexedDidDto);

            indexedDid = await _indexedDidRepository.Create(indexedDid);

            response.Success = true;
            response.Id = indexedDid.Did;
            response.Message = "IndexedDid created successfully.";

            return response;
        }
    }
}
