using System;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.EventCategories.Validators
{
    public class UpdateEventCategoriesDtoValidator : AbstractValidator<UpdateEventCategoriesDto>
    {
        private readonly IEventRepository _eventRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IEventCategoriesRepository _eventCategoriesRepository;

        public UpdateEventCategoriesDtoValidator(
            IEventRepository eventRepository,
            ICategoryRepository categoryRepository,
            IEventCategoriesRepository eventCategoriesRepository)
        {
            _eventRepository = eventRepository;
            _categoryRepository = categoryRepository;
            _eventCategoriesRepository = eventCategoriesRepository;

            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("{PropertyName} is required");

            RuleFor(x => x.EventId)
                .NotEmpty().WithMessage("{PropertyName} is required")
                .MustAsync(EventExists)
                .WithMessage("{PropertyName} not found");

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("{PropertyName} is required")
                .MustAsync(CategoryExists)
                .WithMessage("{PropertyName} not found");

            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("{PropertyName} is required");
        }

        private async Task<bool> EventExists(Guid eventId, CancellationToken cancellationToken)
        {
            return await _eventRepository.Exists(eventId);
        }

        private async Task<bool> CategoryExists(Guid categoryId, CancellationToken cancellationToken)
        {
            return await _categoryRepository.Exists(categoryId);
        }
    }
}
