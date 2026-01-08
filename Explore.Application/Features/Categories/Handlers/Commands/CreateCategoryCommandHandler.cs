using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Category.Validators;
using Explore.Application.Features.Categories.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.Categories.Handlers.Commands
{
    public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, BaseCommandResponse<Guid>>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;

        public CreateCategoryCommandHandler(
            ICategoryRepository categoryRepository,
            IMapper mapper)
        {
            _categoryRepository = categoryRepository;
            _mapper = mapper;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            var validator = new CreateCategoryDtoValidator(_categoryRepository);
            var validationResult = await validator.ValidateAsync(request.CategoryDto);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "Category creation failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            var category = _mapper.Map<Category>(request.CategoryDto);

            category = await _categoryRepository.Create(category);

            response.Success = true;
            response.Id = category.Id;
            response.Message = "Category created successfully.";

            return response;
        }
    }
}
